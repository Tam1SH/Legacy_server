use std::{env, path::Path, fs};

use crate::actix_config::AppConfig;


pub fn config() -> AppConfig {

	let store = {

		let temp = env::temp_dir();
		let store_injector = temp.join("injector");

		let store_injector = Path::new(&store_injector);
		
		if !store_injector.exists() {
			fs::create_dir_all(store_injector).unwrap();
		}

		env::temp_dir()
	};

	AppConfig {
		store
	}

}

