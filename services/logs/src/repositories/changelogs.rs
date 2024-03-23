use std::collections::HashMap;

use async_trait::async_trait;
use deadpool::managed::Object;
use deadpool_redis::Connection;
use crate::model::db::changelogs::ChangelogDb;
use crate::pool::deadpool_surdb::Manager;
use crate::{model::changelogs::Changelog, error::Error};
use crate::repositories::utils::RedisString;
use serde::de::DeserializeOwned;

pub struct ChangelogsRepository<'a> {
	cache: &'a mut Connection,
	db : &'a sqlx::Pool<sqlx::Postgres>
}

impl<'a> ChangelogsRepository<'a> {
	pub fn new(
		cache: &'a mut Connection,
		db : &'a sqlx::Pool<sqlx::Postgres>
	) -> ChangelogsRepository<'a> {
		ChangelogsRepository { cache, db }
	}
}

#[async_trait]
pub(crate) trait CacheInternal {

	async fn get_cache_through_builder<T>(&mut self, builder : &mut KeyForCacheBuilder) -> Result<T, Error>
	where 
		T : DeserializeOwned + Clone;

	async fn get_cache(&mut self, offset: usize, count: usize) -> Result<Vec<Changelog>, Error>;

	async fn write_cache(&mut self, changelogs: &[Changelog], offset: usize) -> Result<(), Error>;

	async fn invalidate_cache(&mut self, builder : &mut KeyForCacheBuilder) -> Result<(), Error>;
}

#[async_trait]
pub trait ChangelogsRepositoryProvider {

	async fn change_exist_changelog(&mut self, new_changelog: &Changelog) -> Result<(), Error>;
	
	async fn get_last_changelog(&mut self) -> Result<Changelog, Error>;

	async fn upload_changelog(&mut self,  changelog: &Changelog) -> Result<(), Error>;

	async fn delete_changelog_by_version(&mut self, version: String) -> Result<(), Error>;

	async fn get_changelogs_count(&mut self) -> Result<usize, Error>;

	async fn get_changelogs(&mut self, offset: usize, count: usize) -> Result<Vec<Changelog>, Error>;

	async fn get_changelog_by_version(&mut self, version: String) -> Result<Changelog, Error>;
}


#[async_trait]
impl ChangelogsRepositoryProvider for ChangelogsRepository<'_> {

	async fn change_exist_changelog(&mut self, new_changelog: &Changelog) -> Result<(), Error> {

		let (en, ru) = get_descriptions(&new_changelog.descriptions)?;

		let mut trans = self.db.begin().await?;
		
		sqlx::query!(
			"DELETE FROM changelogs WHERE version = $1",
			new_changelog.version.clone()
		)
		.execute(&mut *trans)
		.await?;

		sqlx::query!(
			"INSERT INTO changelogs (version, descriptions, create_date) VALUES ($1, json_build_object('ru', $2::text, 'en', $3::text), $4)",
			new_changelog.version.clone(),
			ru,
			en,
			new_changelog.create_date.naive_utc()
		)
		.execute(&mut *trans)
		.await?;

		trans.commit().await?;

		
		self.invalidate_cache(
			KeyForCacheBuilder::new()
				.version(new_changelog.version.clone())
		).await?;

		Ok(())
	}



	async fn upload_changelog(&mut self, changelog: &Changelog) -> Result<(), Error> {

		let _ = get_descriptions(&changelog.descriptions)?;

		sqlx::query_file!(
			"src/sql/cl/create.sql", 
			changelog.version.clone(), 
			serde_json::to_value(&changelog.descriptions).unwrap(), //serde_json::to_string(&changelog.descriptions).unwrap(), 
			changelog.create_date.naive_utc()
		)
		.execute(self.db)
		.await?;
		
		self.invalidate_cache(
			KeyForCacheBuilder::new()
				.count()
		).await?;


		Ok(())
	}
	
	async fn get_last_changelog(&mut self) -> Result<Changelog, Error> {

		//Здесь инвариант по сути то, что в редисе ключ к ченждлогам должен начинаться с cl:0
		let changelog = 
			self.get_cache_through_builder(
				KeyForCacheBuilder::new().index(0)
		)
		.await
		.ok();
	
		if let Some(changelog) = changelog {
			Ok(changelog)
		}
		else {
			
			let result = sqlx::query_as!(
				ChangelogDb,
				"SELECT * FROM changelogs ORDER BY create_date DESC LIMIT 1"
			)
			.fetch_one(self.db)
			.await;

			let result: Changelog = match result {
				Ok(changelog) => { 
					changelog.into()
				},
				Err(sqlx::Error::RowNotFound) => {
					Err(Error::Error("changelogs is empty"))?
				},
				Err(err) => {
					Err(Error::ErrorStr(format!("Database error: {}", err)))?
				}
			};
	
			self.write_cache(&[result.clone()], 0).await?;
			
			Ok(result)
		}
	}

	async fn delete_changelog_by_version(&mut self, version: String) -> Result<(), Error> {

		sqlx::query!(
			"DELETE FROM changelogs WHERE version = $1",
			version
		)
		.execute(self.db)
		.await?;

		self.invalidate_cache(
		KeyForCacheBuilder::new()
			.version(version)
		)
		.await?;

		Ok(())
	}

	async fn get_changelogs_count(&mut self) -> Result<usize, Error> {

		let changelog = 
			self.get_cache_through_builder(
				KeyForCacheBuilder::new().count()
			)
			.await
			.ok();
		
		
		if let Some(changelog) = changelog {
			Ok(changelog)
		}
		else {
			let count = sqlx::query!(
				"SELECT COUNT(*) FROM changelogs"
			)
			.fetch_one(self.db)
			.await?
			.count
			.ok_or_else(|| Error::Error("cant get count"))?;
			
			redis::pipe()
				.set("cl:count", count)
				.expire("cl:count", 60 * 60 * 3)
				.query_async(self.cache).await?;

			Ok(count as usize)
		}

	}

	async fn get_changelog_by_version(&mut self, version: String) -> Result<Changelog, Error> {

		let result = sqlx::query_as!(
			ChangelogDb,
			"SELECT * FROM changelogs WHERE version = $1",
			version
		)
		.fetch_one(self.db)
		.await;
		
		match result {
			Ok(changelog) => { 
				Ok(changelog.into())
			},
			Err(sqlx::Error::RowNotFound) => {
				Err(Error::Error("version was not found"))
			},
			Err(err) => {
				Err(Error::ErrorStr(format!("Database error: {}", err)))
			}
		}
		
	}

	async fn get_changelogs(&mut self, offset: usize, count: usize) -> Result<Vec<Changelog>, Error> {

		let cached_logs = self
			.get_cache(offset, count)
			.await
			.ok();
		
		let fetch_needed = cached_logs
			.as_ref()
			.map_or(true, |logs| logs.len() != count);

		if fetch_needed {
		
			let logs: Vec<Changelog> = sqlx::query_as!(
				ChangelogDb,
				"SELECT * FROM changelogs ORDER BY create_date DESC LIMIT $1 OFFSET $2",
				count as i64,
				offset as i64
			)
			.fetch_all(self.db)
			.await?
			.into_iter()
			.map(Into::into)
			.collect();
		
			
			self.write_cache(&logs, offset).await?;

			Ok(logs)
 		}
		else {
			match cached_logs {
				Some(logs) => Ok(logs),
				None => Err(Error::Error("This will never happen (i believe)"))
			}
		}

	}
	
}


#[async_trait]
impl CacheInternal for ChangelogsRepository<'_> {
	
	
	async fn invalidate_cache(&mut self, builder : &mut KeyForCacheBuilder) -> Result<(), Error> { 

		redis::pipe()
			.unlink("cl:count")
			.query_async::<_, ()>(self.cache)
			.await?;
		
		let key = redis::cmd("KEYS")
			.arg(builder.build())
			.query_async::<_, Vec<RedisString>>(self.cache)
			.await
			.ok();
	
		if let Some(key) = key.and_then(|k| k.first().cloned()) {
			let mut pipe = redis::pipe();
			
			pipe.unlink(key.to_str())
				.query_async::<_, ()>(self.cache)
				.await?;
		};

		Ok(())
	}

	async fn get_cache_through_builder<T>(&mut self, builder : &mut KeyForCacheBuilder) -> Result<T, Error>
		where 
			T : DeserializeOwned + Clone
	{
		let mut pipe = redis::pipe();
		
		if builder.is_count() {
			let value = pipe
				.get("cl:count")	
				.query_async::<_, Vec<RedisString>>(self.cache)
				.await?
				.pop()
				.ok_or_else(|| Error::Error("Error occurs on get 'cl:count'"))?;
			
			return Ok(serde_json::from_str::<T>(value.to_str())?);
		};

		if let Some(key) = redis::cmd("KEYS")
			.arg(builder.build())
			.query_async::<_, Vec<RedisString>>(self.cache)
			.await?
			.pop() 
		{
			pipe.json_get(key.to_str(), "$")?;
		}
		
		pipe
			.query_async::<_, Vec<Option<RedisString>>>(self.cache)
			.await?
			.first()
			.cloned()
			.flatten()
			.and_then(|val| {
				val.deserialize::<T>().ok()
			})
			.ok_or_else(|| Error::Error("value is empty"))
	}


	async fn get_cache(&mut self, offset: usize, count: usize) -> Result<Vec<Changelog>, Error> {
		let mut logs = Vec::new();
	
		for i in offset..(count + offset) {

			let result = self.get_cache_through_builder(
				KeyForCacheBuilder::new().index(i)
			).await;
	
			if let Ok(log) = result {
				logs.push(log);
			}
			
		}
	
		Ok(logs)
	}

	async fn write_cache(&mut self, changelogs: &[Changelog], offset: usize) -> Result<(), Error> {
		let mut pipe = redis::pipe();
			
		for (i, log) in changelogs.iter().enumerate() {
			
			let key = format!("cl:{}:{}", i + offset, &log.version);

			let _ = pipe.json_set(&key, "$", log);
			//dbg!("write cache", &key, log);

			pipe.expire(key, 60 * 60 * 3);
			
		};
		
		pipe.query_async(self.cache).await?;
		Ok(())
	}
}

fn get_descriptions(descriptions: &HashMap<String, String>) -> Result<(&String, &String), Error> {
	Ok((
		descriptions.get("en").ok_or_else(|| Error::Error("english log is empty"))?,
		descriptions.get("ru").ok_or_else(|| Error::Error("russian log is empty"))?
	))
}

pub(crate) struct KeyForCacheBuilder {
	index: Option<usize>,
	version: Option<String>,
	count : bool,
}


impl KeyForCacheBuilder {

	pub fn new() -> Self {
		KeyForCacheBuilder {
			index : None,
			version : None,
			count : false
		}
	}

	pub fn is_count(&self) -> bool {
		self.count
	}

	pub fn index(&mut self, i: usize) -> &mut Self {
		self.index = Some(i);
		self
	}
	
	pub fn count(&mut self) -> &mut Self {
		self.count = true;
		self
	}

	pub fn version(&mut self, version: String) -> &mut Self {
		self.version = Some(version);
		self 
	}

	pub fn build(&self) -> String {
		if let Some(version) = &self.version {
			return format!("cl:*:{}", version);
		}
		if let Some(index) = self.index {
			return format!("cl:{}:*", index);
		}
		if self.count {
			return "cl:count".to_string();
		}
	
		panic!("failed key for cache build")
	}
}


pub async fn flush_cache(conn: &mut Connection) {
	redis::cmd("FLUSHALL")
		.arg("SYNC")
		.query_async::<_, ()>(conn)
		.await
		.unwrap();
}


#[cfg(test)]
mod tests {

	use std::collections::HashMap;
	use serial_test::serial;
	
	
	use crate::test_env::{
		config::*,
		pool::*
	};

	use crate::{
		repositories::changelogs::{
				ChangelogsRepository, 
				ChangelogsRepositoryProvider, 
				CacheInternal, 
				KeyForCacheBuilder, 
				flush_cache
			}, 
		model::changelogs::Changelog, with_pools_serial, actix_config::AppConfig
	};


	with_pools_serial!(delete_changelog_test, delete_changelog_func);
	with_pools_serial!(cache_miss, cache_miss_func);
	with_pools_serial!(partial_cache_miss, partial_cache_miss_func);
	with_pools_serial!(get_changelogs, get_changelogs_func);
	with_pools_serial!(cache_hit, cache_hit_func);
	with_pools_serial!(found_version, found_version_func);
	with_pools_serial!(get_last_changelog, get_last_changelog_func);
	with_pools_serial!(upload_changelog, upload_changelog_func);
	with_pools_serial!(get_changelogs_count, get_changelogs_count_func);
	with_pools_serial!(change_exist_changelog, change_exist_changelog_func);
	with_pools_serial!(not_found_version, not_found_version_func);

	async fn change_exist_changelog_func(cache: &mut deadpool_redis::Connection, _: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>) {

		let mut rep = ChangelogsRepository::new(cache, db);
		let mut map = HashMap::new();

		map.insert("en".to_string(), "TEST".to_string());
		map.insert("ru".to_string(), "TEST".to_string());

		rep.change_exist_changelog(&Changelog { 
			version: "99.99.99 JOPA".to_string(),
			create_date: chrono::Utc::now(), 
			descriptions: map.clone()
		}).await.unwrap();

		rep.change_exist_changelog(&Changelog { 
			version: "IM NOT EXIST AAAAAAH!!!!".to_string(),
			create_date: chrono::Utc::now(), 
			descriptions: map.clone()
		}).await.unwrap();

	}
	
	async fn cache_miss_func(cache: &mut deadpool_redis::Connection, _: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>) {

		let mut rep = ChangelogsRepository::new(cache, db);
		let missed = rep.get_cache(5, 10).await.unwrap();
		
		assert_eq!(0, missed.len());
	}

	async fn partial_cache_miss_func(cache: &mut deadpool_redis::Connection, _: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>) {
	
		let mut rep = ChangelogsRepository::new(cache, db);
		let _ = rep.get_changelogs(0, 10).await.unwrap();
	
		let par_missed = rep.get_cache(5, 10).await.unwrap();
		
		assert_eq!(5, par_missed.len());
	}
	
	async fn get_changelogs_func(cache: &mut deadpool_redis::Connection, _: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>) {

		let mut rep = ChangelogsRepository::new(cache, db);
		let logs = rep.get_changelogs(10, 100).await.unwrap();
	
		assert_eq!(90, logs.len());
		assert!(logs.iter().any(|log| log.version == *"89.89.89 JOPA"));
		
		let logs = rep.get_changelogs(0, 10).await.unwrap();
		
		assert_eq!(10, logs.len());
		assert!(logs.iter().any(|log| log.version == *"90.90.90 JOPA"));
	}

	async fn delete_changelog_func(
		
		cache: &mut deadpool_redis::Connection,
		_: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>
	) {
		
		let mut rep = ChangelogsRepository::new(cache, db);
		let old_logs = rep.get_changelogs(0, 10).await.unwrap();
	
		rep.delete_changelog_by_version("99.99.99 JOPA".to_string()).await.unwrap();
	
		let new_logs = rep.get_changelogs(0, 10).await.unwrap();
	
		assert!(old_logs != new_logs);
	
	}

	
	async fn cache_hit_func(cache: &mut deadpool_redis::Connection, _: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>) {

		let mut rep = ChangelogsRepository::new(cache, db);
		let _ = rep.get_changelogs(0, 10).await.unwrap();
	
		let cache_hit = rep.get_cache(0, 10).await.unwrap();
		
		assert_eq!(10, cache_hit.len());
	
		let _ = rep.get_changelogs(50, 100).await.unwrap();
	
		let cache_hit = rep.get_cache(50, 100).await.unwrap();
		
		assert_eq!(50, cache_hit.len());
	
		let cache_hit = rep.get_cache(0, 10).await.unwrap();
		
		assert_eq!(10, cache_hit.len());
	
		let cache_hit = rep.get_cache(0, 100).await.unwrap();
		
		assert_eq!(60, cache_hit.len());
	}
	
	async fn found_version_func(cache: &mut deadpool_redis::Connection, _: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>) {
		let mut rep = ChangelogsRepository::new(cache, db);
		let log = rep.get_changelog_by_version("99.99.99 JOPA".to_string()).await;
		assert!(log.is_ok());
		let log = rep.get_changelog_by_version("98.98.98 JOPA".to_string()).await;
		assert!(log.is_ok());
		let log = rep.get_changelog_by_version("97.97.97 JOPA".to_string()).await;
		assert!(log.is_ok());
	}

	async fn not_found_version_func(cache: &mut deadpool_redis::Connection, _: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>) {
		let mut rep = ChangelogsRepository::new(cache, db);
		let log = rep.get_changelog_by_version("99.99.99 JOPA1 ".to_string()).await;
		assert!(log.is_err());
		let log = rep.get_changelog_by_version("".to_string()).await;
		assert!(log.is_err());
		let log = rep.get_changelog_by_version("etrjhtukukdtuy[pok".to_string()).await;
		assert!(log.is_err());
	}
	
	async fn get_changelogs_count_func(cache: &mut deadpool_redis::Connection, _: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>) {
		let mut rep = ChangelogsRepository::new(cache, db);
		let count = rep.get_changelogs_count().await.unwrap();
		let cache: usize = rep.get_cache_through_builder(
			KeyForCacheBuilder::new().count()
		).await.unwrap();
		assert_eq!(100, count);
		assert!(cache == count);
		rep.delete_changelog_by_version("99.99.99 JOPA".to_string()).await.unwrap();
		let count = rep.get_changelogs_count().await.unwrap();
		let cache: usize = rep.get_cache_through_builder(
			KeyForCacheBuilder::new().count()
		).await.unwrap();
		assert_eq!(99, count);
		assert!(cache == count);
	}
	
	async fn upload_changelog_func(cache: &mut deadpool_redis::Connection, _: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>) {

		let mut rep = ChangelogsRepository::new(cache, db);
		let mut map = HashMap::new();

		map.insert("en".to_string(), "TEST".to_string());
		map.insert("ru".to_string(), "TEST".to_string());

		rep.upload_changelog(
			&Changelog { 
				version: "2.2.8 LOL".to_string(), 
				create_date: chrono::Utc::now(), 
				descriptions: map.clone()
		}).await.unwrap();

		let is_err = rep.upload_changelog(
			&Changelog { 
				version: "2.2.8 LOL".to_string(), 
				create_date: chrono::Utc::now(), 
				descriptions: map
		}).await;

		assert!(is_err.is_err());

		let cache = rep.get_cache_through_builder::<Changelog>(
			KeyForCacheBuilder::new().index(0)
		).await;

		assert!(cache.is_err());

		rep.get_changelog_by_version("2.2.8 LOL".to_string()).await.unwrap();

		let mut map = HashMap::new();

		map.insert("en".to_string(), "TEST".to_string());
		map.insert("ru".to_string(), "TEST".to_string());

		let is_err = rep.upload_changelog(
			&Changelog { 
				version: "99.99.99 JOPA".to_string(), 
				create_date: chrono::Utc::now(), 
				descriptions: map
			}).await;

		assert!(is_err.is_err());
	}
	
	async fn get_last_changelog_func(cache: &mut deadpool_redis::Connection, _: &AppConfig, db : &sqlx::Pool<sqlx::Postgres>) {
		let mut rep = ChangelogsRepository::new(cache, db);
		let log = rep.get_last_changelog().await.unwrap();
		let cache: Changelog = rep.get_cache_through_builder(
			KeyForCacheBuilder::new().index(0)
		).await.unwrap();
		assert!(log.version == *"99.99.99 JOPA");
		assert!(cache == log);
		rep.delete_changelog_by_version("99.99.99 JOPA".to_string()).await.unwrap();
		let emtry_cache = rep.get_cache_through_builder::<Changelog>(
			KeyForCacheBuilder::new().index(0)
		).await;
		assert!(emtry_cache.is_err());
		let log = rep.get_last_changelog().await.unwrap();
		let cache: Changelog = rep.get_cache_through_builder(
			KeyForCacheBuilder::new().index(0)
		).await.unwrap();
		assert!(log.version == *"98.98.98 JOPA");
		assert!(cache == log);
		let log = rep.get_last_changelog().await.unwrap();
		let cache: Changelog = rep.get_cache_through_builder(
			KeyForCacheBuilder::new().index(0)
		).await.unwrap();
		assert!(log.version == *"98.98.98 JOPA");
		assert!(cache == log);
	}

}

