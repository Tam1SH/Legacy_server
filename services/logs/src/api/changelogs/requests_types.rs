
use chrono::{DateTime, Utc};
use utoipa::ToSchema;
use serde::Deserialize;

use actix_multipart::
    form::{
        tempfile::TempFile,
        MultipartForm, text::Text
    };


#[derive(Deserialize, ToSchema)]
pub struct GetChangeLogsModel {
	pub offset: usize,
	pub count: usize
}

#[derive(Debug, Deserialize, ToSchema)]
pub struct GetChangeLogModel {
	pub version: String,
}
#[allow(non_snake_case)]
#[derive(Debug, ToSchema, MultipartForm)]
pub struct ChangeLogUploadModel
{
	#[schema(value_type = String)]
	pub version: Text<String>,
	#[schema(value_type = String, format = "date-time")]
	pub createDate: Text<DateTime<Utc>>,
	
	#[schema(value_type = Option<bool>)]
	pub ChangeInDatabase : Option<Text<bool>>,
	#[schema(value_type = Option<bool>)]
	pub Publish: Option<Text<bool>>,


	#[schema(value_type = String)]
	//https://github.com/actix/actix-web/pull/2883
	//В общем здесь HashMap<String, String>
	pub data: Text<String>,

    #[multipart(rename = "injector")]
	#[schema(value_type = String, format = Binary)]
    pub injector: Vec<TempFile>
}



