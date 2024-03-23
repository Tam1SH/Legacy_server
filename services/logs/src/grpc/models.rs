
use crate::grpc::api::{
	log::{
		MessageBody as gen_MessageBody, 
		message_body::UserId as gen_UserId,
		message_body::AdditionalData as gen_AdditionalData,
	},
	Log as gen_Log
};

use serde::{Serialize, Deserialize};
use surrealdb::sql::{Thing, Id};

#[derive(Debug, Serialize, Deserialize, Default)]
pub struct AdditionalData {
	pub message: String,
	pub flags : Vec<i32>,
}

impl From<gen_AdditionalData> for AdditionalData {
	fn from(value: gen_AdditionalData) -> Self {
		Self { 
			message: value.message,
			flags : value.flags
		}
	}
} 

impl From<AdditionalData> for gen_AdditionalData {
	fn from(value: AdditionalData) -> Self {
		Self { 
			message: value.message,
			flags : value.flags
		}
	}
} 

#[derive(Debug, Serialize, Deserialize)]
pub struct UserId {
	pub user_id : i64
}

impl From<gen_UserId> for UserId {
	fn from(value: gen_UserId) -> Self {
		Self { 
			user_id: value.user_id 
		}
	}
}

impl From<UserId> for gen_UserId {
	fn from(value: UserId) -> Self {
		Self { 
			user_id: value.user_id 
		}
	}
}


#[derive(Debug, Serialize, Deserialize, Default)]
pub struct MessageBody {
	pub title: String,
	pub message: String,
	#[serde(skip_serializing_if = "Option::is_none")]
	pub user_id: Option<UserId>,
	#[serde(skip_serializing_if = "Option::is_none")]
	pub additional_data: Option<AdditionalData>,
}

impl From<MessageBody> for gen_MessageBody {
	fn from(value: MessageBody) -> Self {
		Self {
			additional_data : value.additional_data.map(Into::into),
			message : value.message,
			title : value.title,
			user_id : value.user_id.map(Into::into)
		}
	}
}

impl From<gen_MessageBody> for MessageBody {
	fn from(value: gen_MessageBody) -> Self {
		Self {
			additional_data : value.additional_data.map(Into::into),
			message : value.message,
			title : value.title,
			user_id : value.user_id.map(Into::into)
		}
	}
}


#[derive(Debug, Serialize, Deserialize)]
pub struct Log { 
	pub id : Thing,
	pub level: i32,
	pub timestamp: i64,
	pub importance : i32,
	pub service_name: String,
	pub controller_name: String,
	pub message: MessageBody,
}


impl From<Log> for gen_Log {
	fn from(value: Log) -> Self {
		Self {
			id : value.id.id.to_raw().parse().unwrap(),
			controller_name : value.controller_name,
			importance : value.importance,
			level : value.level,
			message : Some(value.message.into()),
			service_name : value.service_name,
			timestamp : value.timestamp
		}
	}
}

impl From<gen_Log> for Log {
	fn from(value: gen_Log) -> Self {
		
		Self {
			id : ("".to_string(), Id::Number(value.id)).into(),
			controller_name : value.controller_name,
			importance : value.importance,
			level : value.level,
			message : value.message
				.map(Into::into)
				.or(Default::default())
				.unwrap(),
			service_name : value.service_name,
			timestamp : value.timestamp
		}
	}
}


