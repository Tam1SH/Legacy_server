

use redis::{FromRedisValue, RedisError, ErrorKind};
use derive_more::{Into, Display};
use serde::de::DeserializeOwned;

use crate::error::Error;


#[derive(Debug, Display, Into, Clone)]
pub struct RedisString(String);

impl FromRedisValue for RedisString {
	fn from_redis_value(v: &redis::Value) -> redis::RedisResult<Self> {
		
		match v {
			redis::Value::Data(data) => {
				Ok(RedisString(String::from_utf8(data.to_vec()).unwrap()))
			},
			// redis::Value::Bulk(data) => {
			// 	let strs: Vec<String> = data.into_iter().map(|item| {
			// 		match item {
			// 			redis::Value::Data(data) => String::from_utf8(data.to_vec()).unwrap(),
			// 			redis::Value::Status(s) => s.to_string(),
			// 			redis::Value::Nil => "nil".to_string(),
			// 			redis::Value::Okay => "OK".to_string(),
			// 			redis::Value::Int(num) => num.to_string(),
			// 			_ => "".to_string(),
			// 		}
			// 	}).collect();
				
			// 	// Convert the vector of strings into a JSON array
			// 	let json_array = serde_json::to_string(&strs).unwrap();
				
			// 	Ok(RedisString(json_array))
			// },
			redis::Value::Status(s) => Ok(RedisString(s.to_string())),
			redis::Value::Nil => Ok(RedisString("nil".to_string())),
			redis::Value::Okay => Ok(RedisString("OK".to_string())),
			redis::Value::Int(num) => Ok(RedisString(num.to_string())),
			_ => Err(RedisError::from((ErrorKind::Serialize, "")))
		}
	}
}

impl RedisString {

	pub fn deserialize<T : DeserializeOwned + Clone>(&self) -> Result<T, Error> {
		serde_json::from_str::<Vec<T>>(&self.0)?
			.first()
			.cloned()
			.ok_or_else(|| Error::Error("value is empty"))
	}

	pub fn to_str(&self) -> &String {
		&self.0
	}

	pub fn new(str : String) -> Self {
		Self(str)
	}
}
