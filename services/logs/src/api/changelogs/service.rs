use std::{io::{Write}, path::{PathBuf}};
use async_trait::async_trait;
use chrono::Utc;
use deadpool::managed::Object;
use std::fs::File as StdFile;
use tokio::{fs::File, io::{AsyncReadExt, AsyncWriteExt}};

use crate::{
	error::Error, 
	repositories::changelogs::{
		ChangelogsRepository, 
		ChangelogsRepositoryProvider
	}, 
	pool::deadpool_surdb::Manager, 
	model::changelogs::Changelog, grpc::changelogs::{client::get_client, generated}, actix_config::AppConfig
};
use super::response_types::GetChangeLogsResult;

#[async_trait]
pub trait ChangelogServiceProvider {

	async fn get_changelogs(&mut self, offset: usize, count: usize, lang: Option<String>) -> Result<Vec<GetChangeLogsResult>, Error>;
	
	async fn get_changelogs_count(&mut self) -> Result<usize, Error>;

	async fn delete_changelog_by_version(&mut self, version: String) -> Result<(), Error>;

	async fn get_changelog_by_version(&mut self, version: String) -> Result<Vec<GetChangeLogsResult>, Error>;

	async fn upload_changelog(&mut self, changelog: Changelog, change_in_database: bool, publish: bool, file : StdFile) -> Result<(), Error>;

	async fn change_exist_changelog(&mut self, new_changelog: Changelog) -> Result<(), Error>;

}

pub struct ChangelogService<'a> {
	db : &'a sqlx::Pool<sqlx::Postgres>,
	cache: &'a mut deadpool_redis::Connection,
	config : &'a AppConfig
}

#[async_trait]
impl ChangelogServiceProvider for ChangelogService<'_> {

	async fn change_exist_changelog(&mut self, new_changelog: Changelog) -> Result<(), Error> {
		Ok(ChangelogsRepository::new(self.cache, self.db)
			.change_exist_changelog(&new_changelog)
			.await?)
			
	}

	async fn upload_changelog(
		&mut self, 
		changelog: Changelog, 
		change_in_database: bool, 
		publish: bool,
		file : StdFile
	) -> Result<(), Error> {
	
		let mut client = get_client().await?;

		let mut path = self.config.store.clone();
		path.push("injector/Sasavn Injector.exe");
		write_to_disk(File::from_std(file), path).await?;
			
		let response = client.upload_changelog(generated::Changelog {
			change_in_database,
			publish,
			data : changelog.descriptions,
			timestamp : Utc::now().timestamp(),
			version : changelog.version.clone()
		}).await;

		match response {
			Ok(response) => {
				if response.get_ref().result == 0 {
					Ok(())
				}
				else {
					Err(Error::ErrorStr(format!("error {}", response.get_ref().error)))
				}
			},
			Err(status) => {
				Err(Error::ErrorStr(format!("error {}", status)))
			}
		}
	}

	async fn get_changelog_by_version(&mut self, version: String) -> Result<Vec<GetChangeLogsResult>, Error> {

		let Changelog {
			create_date, 
			descriptions,
			version
		} = ChangelogsRepository::new(self.cache, self.db)
			.get_changelog_by_version(version)
			.await?;
	
		Ok(
			descriptions
			.iter()
			.map(|result| GetChangeLogsResult {
				create_date,
				data : result.1.clone(),
				language : result.0.clone(),
				version : version.clone()
			})
			.collect()
		)

	}
	async fn delete_changelog_by_version(&mut self, version: String) -> Result<(), Error> {
		
		ChangelogsRepository::new(self.cache, self.db)
			.delete_changelog_by_version(version)
			.await

	}
	
	async fn get_changelogs_count(&mut self) -> Result<usize, Error> {
		
		ChangelogsRepository::new(self.cache, self.db)
			.get_changelogs_count()
			.await
		
	}

	async fn get_changelogs(&mut self, offset: usize, count: usize, lang: Option<String>) -> Result<Vec<GetChangeLogsResult>, Error> {

		let logs = ChangelogsRepository::new(self.cache, self.db)
			.get_changelogs(offset, count)
			.await?;
		
		let get_result = |log| format_log_to_result(log, &lang);
		
		logs
			.iter()
			.map(get_result)
			.collect()
	}
}

async fn write_to_disk(mut file: File, src: PathBuf) -> Result<(), Error> {

	let path = src
		.to_str()
		.ok_or_else(|| Error::Error("invalid path"))?;

	let mut file_desc = File::create(path).await?;

	let mut buffer = vec![];
	
	file.read_to_end(&mut buffer).await?;
	file_desc.write_all(&buffer).await?;

	Ok(())
}

fn format_log_to_result(log: &Changelog, lang: &Option<String>) -> Result<GetChangeLogsResult, Error> {
	
	let lang = match &lang {
		Some(lang) => {
			if lang == "ch" || lang == "pl" { "en" } 
			else { lang	}
		},
		None => "en",
	};

	let (create_date, version) = (
		log.create_date,
		log.version.clone()
	);

	let data = log.descriptions
		.get(lang)
		.ok_or_else(|| Error::Error("can't find description"))?
		.to_string();

	Ok(GetChangeLogsResult { 
		create_date, 
		data, 
		version, 
		language: lang.to_string()
	})
}


impl<'a> ChangelogService<'a> {
	pub fn new(
		db : &'a sqlx::Pool<sqlx::Postgres>,
		cache: &'a mut deadpool_redis::Connection,
		config: &'a AppConfig,
	) -> Self {

		ChangelogService {
			db, cache, config
		}
	}
}

#[cfg(test)]
mod tests {
	
	use std::{collections::HashMap, fs::{File, self}, io::Write};
	use deadpool::managed::Object;
	use serial_test::serial;
	use crate::{pool::deadpool_surdb::Manager, repositories::changelogs::flush_cache, with_pools, model::changelogs::Changelog, with_pools_serial, actix_config::AppConfig};
	use super::{ChangelogService, ChangelogServiceProvider};

	use crate::test_env::{
		config::*,
		pool::*
	};
	
	
    with_pools!(get_change_logs_per_lang, get_change_logs_per_lang_func);
    with_pools!(get_change_logs_where_count_is_smaller_than_required, get_change_logs_where_count_is_smaller_than_required_func);
	with_pools_serial!(upload_changelog, upload_changelog_func);


    async fn get_change_logs_per_lang_func(cache: &mut deadpool_redis::Connection, config: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>) {
        
		let mut service = ChangelogService::new(db, cache, config);
        
		let logs = service.get_changelogs(0, 1, Some("en".to_string())).await.unwrap();
        
        assert_eq!(logs[0].version, "99.99.99 JOPA".to_string());
        
		let text_en = "Tincidunt lobortis feugiat vivamus at. Sapien faucibus et molestie ac feugiat. Ac tortor vitae purus faucibus ornare. At erat pellentesque adipiscing commodo elit at imperdiet dui. Laoreet non curabitur gravida arcu ac tortor dignissim convallis. Maecenas ultricies mi eget mauris pharetra et. Ultrices neque ornare aenean euismod elementum nisi. Justo donec enim diam vulputate. Turpis egestas sed tempus urna et pharetra pharetra massa massa. A iaculis at erat pellentesque adipiscing commodo elit at. Tellus orci ac auctor augue mauris augue. Ultrices dui sapien eget mi. Orci ac auctor augue mauris augue. Ut pharetra sit amet aliquam id. Sit amet venenatis urna cursus eget nunc scelerisque viverra. Sapien pellentesque habitant morbi tristique senectus et netus. Fames ac turpis egestas integer.";
        
		assert_eq!(text_en, logs[0].data);
        
		let logs = service.get_changelogs(0, 1, Some("ru".to_string())).await.unwrap();
        
        assert_eq!(logs[0].version, "99.99.99 JOPA".to_string());
        
		let text_ru = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Velit scelerisque in dictum non consectetur a erat nam. Imperdiet proin fermentum leo vel orci porta non pulvinar. Est sit amet facilisis magna etiam tempor. Eget nullam non nisi est. Tellus rutrum tellus pellentesque eu tincidunt tortor aliquam nulla. Dignissim convallis aenean et tortor. Fermentum posuere urna nec tincidunt praesent semper. Sed velit dignissim sodales ut eu sem integer vitae. Gravida neque convallis a cras semper auctor. A iaculis at erat pellentesque adipiscing commodo.";
        assert_eq!(text_ru, logs[0].data);

		
		let logs_1 = service.get_changelogs(0, 1, Some("pl".to_string())).await.unwrap();
		let logs_2 = service.get_changelogs(0, 1, Some("ch".to_string())).await.unwrap();
		
		assert_eq!(text_en, logs_1[0].data);
		assert_eq!(text_en, logs_2[0].data);

		let is_error = service.get_changelogs(0, 1, Some("hrthrth___1231gerhcc!#25689".to_string())).await;

		assert!(is_error.is_err());

    }

	async fn upload_changelog_func(cache: &mut deadpool_redis::Connection, config: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>) {

		let mut map = HashMap::new();

		map.insert("en".to_string(), "TEST".to_string());
		map.insert("ru".to_string(), "TEST".to_string());
		
		let temp_dir = config.store.clone();
		
		let mut file = File::create(temp_dir.join("azaza")).unwrap();
		file.write_all(b"Step sister").unwrap();
		let file = File::open(temp_dir.join("azaza")).unwrap();
		
        ChangelogService::new(db, cache, config)
			.upload_changelog(
				Changelog { 
					version: "2.2.8 LOL".to_string(), 
					create_date: chrono::Utc::now(), 
					descriptions: map
				}, 
			false, false, file
			).await.unwrap();
		
		let mut path = config.store.clone();
		path.push("injector/Sasavn Injector.exe");

		let contents = fs::read_to_string(path).unwrap();

		assert_eq!("Step sister", &contents);

    }


    async fn get_change_logs_where_count_is_smaller_than_required_func(
		
		cache: &mut deadpool_redis::Connection, 
		config: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>
	) {
        let logs = ChangelogService::new(db, cache, config).get_changelogs(99, 10, None).await.unwrap();
        assert_eq!(logs.len(), 1);
    }

}