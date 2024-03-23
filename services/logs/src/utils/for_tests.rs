#[macro_export]
macro_rules! with_pools_serial {
	($name:ident, $func:expr) => {
		#[tokio::test]
		#[serial]
		async fn $name() {
			let pools = setup_pools().await;
			let config = config();
			let db = pools.db;
			let mut cache = pools.redis.get().await.unwrap();
			
			flush_cache(&mut cache).await;

			$func(&mut cache, &config, &db).await;

			flush_cache(&mut cache).await;
		}
	};
}

#[macro_export]
macro_rules! with_pools {
	($name:ident, $func:expr) => {
		#[tokio::test]
		async fn $name() {
			let pools = setup_pools().await;
			let config = config();
			let db = pools.db;
			let mut cache = pools.redis.get().await.unwrap();
			
			flush_cache(&mut cache).await;

			$func(&mut cache, &config, &db).await;

			flush_cache(&mut cache).await;
		}
	};
}
