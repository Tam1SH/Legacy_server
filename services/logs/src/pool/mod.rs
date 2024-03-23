pub mod deadpool_surdb;
use deadpool::managed::{Pool, Object};
use deadpool_redis::Manager as RedisManager;
use deadpool::Runtime;
use deadpool_redis::Config;
use deadpool::managed;
use sqlx::postgres::PgPoolOptions;

use crate::error::Error;

use self::deadpool_surdb::{Manager, ConnectionType};


pub type DbPool = managed::Pool<Manager>;

pub fn build_pool(url : &'static str) -> DbPool {
	let mgr = Manager::new(
		url, 
		Some("default"), 
		Some("default"), 
		ConnectionType::Ws
	);

   	DbPool::builder(mgr)
		.build()
		.unwrap()

}



#[derive(Clone)]
pub struct Pools {
	pub db: sqlx::Pool<sqlx::Postgres>,
	pub redis : Pool<RedisManager, deadpool_redis::Connection>
}




pub async fn get_connections(pool : &Pools) -> Result<(
	&sqlx::Pool<sqlx::Postgres>,
	deadpool_redis::Connection,
), Error> {
	Ok((
		&pool.db,
		pool.redis.get().await?,
	))
}

pub async fn setup_pools() -> Pools {
	
	let cfg = Config::from_url(dotenv!("REDIS_URL"));

	let pool = cfg.create_pool(Some(Runtime::Tokio1)).unwrap();

	let url = format!("postgresql://{}:{}@{}/default", 
		dotenv!("POSTGRES_USER"), 
		dotenv!("POSTGRES_PASSWORD"),
		dotenv!("POSTGRES_NAME")
	);
	Pools {
		db : PgPoolOptions::new()
			.max_connections(100)
			.connect(&url).await.unwrap(),
		redis : pool
	}

}

