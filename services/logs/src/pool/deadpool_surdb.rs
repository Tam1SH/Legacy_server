
use async_trait::async_trait;
use deadpool::managed;
use surrealdb::{engine::remote::ws::{Client, Ws, Wss}, Surreal};

pub enum ConnectionType {
	Ws,
	Wss
}
pub struct Manager<S: Into<String> = &'static str> {
    hddr: S,
	ns: Option<S>,
	db: Option<S>,
	conn_type : ConnectionType
}

impl<S> Manager<S> where String: From<S> {
	pub fn new(
		hddr: S,
		ns: Option<S>,
		db: Option<S>,
		conn_type : ConnectionType
	) -> Self {
		Self {
			conn_type, db, ns, hddr
		}
	}
}

#[async_trait]
impl managed::Manager for Manager {
	type Type = Surreal<Client>;
	type Error = surrealdb::Error;
	
	async fn create(&self) -> Result<Self::Type, Self::Error> {
		let client = match self.conn_type {
			ConnectionType::Ws => Surreal::new::<Ws>(self.hddr).await?,
			ConnectionType::Wss => Surreal::new::<Wss>(self.hddr).await?
		};

		let use_ns = self.ns.map(|ns| client.use_ns(ns));
		let use_db = use_ns
			.and_then(|use_ns| self.db.map(|db| use_ns.use_db(db)));

		Ok(
			match use_db {
				Some(use_db) => {
					use_db.await?;
					client
				}, 
				None => client
			}
		)
	}
	
	async fn recycle(&self, con: &mut Self::Type, _: &managed::Metrics) -> managed::RecycleResult<Self::Error> {

		match con.health().await {
			Ok(_) => Ok(()),
			Err(err) => Err(managed::RecycleError::Backend(err))
		}
	}
}