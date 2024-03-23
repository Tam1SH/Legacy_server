
use deadpool::Runtime;
use deadpool_redis::Config;
use sqlx::postgres::PgPoolOptions;
use surrealdb::{Surreal, engine::remote::ws::{Ws, Client}};

use crate::{pool::{Pools, build_pool}, db_config::db_config};

pub async fn setup_pools_bench() -> Pools {
	
	let cfg = Config::from_url(dotenv!("REDIS_URL_LOCAL"));

	let pool = cfg.create_pool(Some(Runtime::Tokio1)).unwrap();

	let db = db_config_bench().await;


	Pools {
		db,
		redis : pool
	}
	
}

pub async fn db_config_bench() -> sqlx::Pool<sqlx::Postgres> {
	
	let url = format!("postgresql://{}:{}@localhost/default", 
		dotenv!("POSTGRES_USER"), 
		dotenv!("POSTGRES_PASSWORD")
	);

	let db = PgPoolOptions::new()
		.max_connections(1000000)
		.connect(&url).await.unwrap();

	sqlx::query_file!(
		"src/sql/tests/clear_cl.sql"
	).execute(&db).await.unwrap();

	sqlx::query_file!(
		"src/sql/tests/cls.sql"
	).execute(&db).await.unwrap();


	db
}

pub async fn setup_pools() -> Pools {
	
	let cfg = Config::from_url(dotenv!("REDIS_URL_LOCAL"));

	let pool = cfg.create_pool(Some(Runtime::Tokio1)).unwrap();

	db_config().await.unwrap();
	
	let url = format!("postgresql://{}:{}@localhost/default", 
		dotenv!("POSTGRES_USER"), 
		dotenv!("POSTGRES_PASSWORD")
	);
	let db = PgPoolOptions::new()
		.max_connections(100)
		.connect(&url).await.unwrap();

	sqlx::query_file!(
		"src/sql/tests/clear_cl.sql"
	).execute(&db).await.unwrap();

	sqlx::query_file!(
		"src/sql/tests/cls.sql"
	).execute(&db).await.unwrap();

	Pools {
		db,
		redis : pool
	}

}
