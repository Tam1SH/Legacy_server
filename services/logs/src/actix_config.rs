use std::io;
use std::path::PathBuf;
use actix_web::{web, middleware, App, HttpServer};
use crate::pool::{setup_pools, Pools};
use crate::api::changelogs::scope::changelog_controller;

pub struct AppConfig {
	pub store: PathBuf
}
pub struct AppState {
	pub pools: Pools,
	pub config: AppConfig
}

pub fn config() -> AppConfig {

	let store = PathBuf::from(dotenv!("STORE"));

	AppConfig {
		store
	}

}
pub async fn config_actix_server() -> Result<actix_web::dev::Server, io::Error> {

	if std::env::var_os("RUST_LOG").is_none() {
        std::env::set_var("RUST_LOG", "actix_web=info");
    }
    env_logger::init();

	let pool = setup_pools().await;

	println!("start at {}", dotenv!("HOST_ACTIX"));
	Ok(
		HttpServer::new(move || {

			App::new()
				.app_data(web::Data::new(AppState {
					config: AppConfig { store: PathBuf::from(dotenv!("STORE")) },
					pools : pool.clone()
				}))
				.wrap(middleware::Compress::default())
				.wrap(middleware::Logger::default())
				.service(
					web::scope("/api")
						.service(changelog_controller())
				)
				
		})
			.bind(dotenv!("HOST_ACTIX"))?
			.run()
	)
}

