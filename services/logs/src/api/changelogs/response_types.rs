
use utoipa::ToSchema;
use chrono::{DateTime, Utc};
use serde::Serialize;


#[derive(Serialize, ToSchema)]
#[serde(rename_all = "camelCase")]
pub struct GetChangeLogsResult {
	pub version: String,
	#[schema(value_type = String, format = "date-time")]
	pub create_date: DateTime<Utc>,
	pub data: String,
	pub language: String
}

