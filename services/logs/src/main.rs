
#[macro_use]
extern crate dotenv_codegen;

pub mod actix_config;
pub mod grpc;
pub mod grpc_config;
pub mod pool;
pub mod db_config;
pub mod model;
pub mod api;
pub mod error;
pub mod utils;
pub mod openapi;
pub mod thirdparty;
pub mod repositories;

#[cfg(test)]
pub mod test_env;


use std::env;

use actix_config::config_actix_server;
use grpc_config::config_grpc_server;

use openapi::openapi;
use tokio::join;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {

    let args: Vec<String> = env::args().collect();
	dbg!(&args);

	if !args.is_empty() && args.iter().any(|arg| *arg == *"dummy") {
		let _ = openapi();
		return Ok(());
	}

	db_config::db_config().await?;

	let grpc = async move {
        tokio::task::spawn(
			config_grpc_server()
        ).await
    };

	let http = config_actix_server().await.expect("http server not started");

	let _ = join!(grpc, http);

	Ok(())
}



