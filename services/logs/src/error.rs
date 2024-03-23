use actix_web::HttpResponse;
use serde::Serialize;
use utoipa::ToSchema;
use std::fmt;
use deadpool::managed::{PoolError, RecycleError};


#[derive(Debug, thiserror::Error)]
pub enum Error {


	#[error(transparent)]
	StdIOError(#[from] std::io::Error),

	#[error(transparent)]
	RedisError(#[from] redis::RedisError),
	
	#[error(transparent)]
	ActixError(#[from] actix_web::Error),

	#[error(transparent)]
	DbError(#[from] surrealdb::Error),

	#[error(transparent)]
	SerdeError(#[from] serde_json::error::Error),

	#[error(transparent)]
	PoolErrorSur(#[from] PoolError<surrealdb::Error>),

	#[error(transparent)]
	PoolErrorRedis(#[from] PoolError<redis::RedisError>),

	#[error("{0}")]
	Error(&'static str),

	#[error("{0}")]
	ErrorStr(String),

	#[error(transparent)]
	TonicError(#[from] tonic::transport::Error),

	#[error(transparent)]
	JsonWebTokenError(#[from] jsonwebtoken::errors::Error),

	#[error(transparent)]
	RecycleError(#[from] RecycleError<surrealdb::Error>),

	#[error(transparent)]
	PgresError(#[from] sqlx::Error)

}

pub type ActixResult<R, E> = actix_web::Result<R, E>;


#[derive(Debug, Serialize, ToSchema)]
pub struct ResponseError {
    pub code: i64,
    pub message: String,
}

impl ResponseError {
	pub fn new<T: Into<String>>(code: i64, message: T) -> Self {
		Self {
			code,
			message : message.into()
		}
	}
}

impl From<Error> for ResponseError {
    fn from(err: Error) -> Self {
        ResponseError {
            code: -1,
            message: err.to_string(),
        }
    }
}

impl fmt::Display for ResponseError {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        write!(f, "code: {}, message: {}", self.code, self.message)
    }
}

impl actix_web::error::ResponseError for ResponseError {

    fn error_response(&self) -> HttpResponse {
        HttpResponse::InternalServerError().json(self)
    }
}