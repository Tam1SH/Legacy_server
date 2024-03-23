use surrealdb::{
	Surreal, 
	engine::remote::ws::{Ws, Client}
};




pub async fn db_config() -> Result<Surreal<Client>, surrealdb::Error> {
	
	let db = if cfg!(test) {
		Surreal::new::<Ws>(dotenv!("DB_URL_MOCK")).await?
	}
	else {
		Surreal::new::<Ws>(dotenv!("DB_URL")).await?
	};

	db.use_ns("default").use_db("default").await?;


	Ok(db)
}