


use std::error;
use chrono::TimeZone;
use serde::Deserialize;
use surrealdb::Surreal;
use surrealdb::engine::remote::ws::{Ws, Client};
use surrealdb::sql::{Thing, Id};
use tonic::{Request, Response, Status};

use crate::grpc::models::Log;
use crate::grpc::api::{
	GetLogsResponse, 
	GetLogsRequest, 
	logs_service_server::LogsService, 
	LogRequest,
	LogResponse,
	Log as gen_Log
};


use chrono::prelude::Utc;

#[derive(Debug, Default)]
pub struct Logs {}

#[tonic::async_trait]
impl LogsService for Logs {

	async fn get_logs(&self, req: Request<GetLogsRequest>) -> Result<Response<GetLogsResponse>, Status> {

		let req = req.get_ref();

		println!("get logs: {}, {}", req.count, req.offset);

		match self.get_logs(req.count, req.offset).await {
			Ok(result) => {
				let logs = result.logs;

				let logs = logs
					.into_iter()
					.map(gen_Log::from)
					.collect();

				Ok(Response::new(
					GetLogsResponse {
						logs, total_size : result.total_size
					}
				))
			},
			Err(err) => 
				Err(Status::new(tonic::Code::Internal, err.to_string()))
			
		}
	}

    async fn log(&self, request: Request<LogRequest>) -> Result<Response<LogResponse>, Status> {
		
		let request = request.get_ref();
		let log = request.clone().log.unwrap();

		let formatted_msg = log
			.message
			.map_or("None".to_string(), |msg| msg.message);

		println!("[{}] [{}] {} {}", 
			Utc.timestamp_millis_opt(log.timestamp).unwrap().format("%H:%M:%S%.3f"), 
			log.level, 
			log.controller_name,
			formatted_msg,
		);
		
		match self._log(request.clone()).await {
			Ok(_) => {

				let reply = LogResponse {
					result: 0, error: "".to_string() 
				};
		
				Ok(Response::new(reply))
			},
			Err(err) => {

				let reply = LogResponse {
					result: -1, error: err.to_string()
				};
		
				Ok(Response::new(reply))
			}
		}

    }


}



#[derive(Debug, Deserialize)]
struct Record {
    #[allow(dead_code)]
    id: Thing,
}

	 

struct GetLogsResult {
	pub logs: Vec<Log>,
	pub total_size : i32,
}

impl Logs {


	async fn set_con(&self) -> Result<Surreal<Client>, Box<dyn error::Error>> {
		let db = Surreal::new::<Ws>("surrealdb:8000").await?;
		db.use_ns("logs").use_db("logs").await?;

		Ok(db)
	}

	async fn get_logs(&self, count : i32, offset: i32) -> Result<GetLogsResult, Box<dyn error::Error>> {
		let db = self.set_con().await?;

		let total_count: Option<i32> = db
			.query("RETURN count(SELECT * FROM logs)")
			.await?
			.take(0)?;

		let logs: Vec<Log> = db
			.query("RETURN SELECT * FROM logs ORDER BY id DESC LIMIT $count START $offset")
			.bind(("count", count))
			.bind(("offset", offset))
			.await?
			.take(0)?;

			
		Ok(GetLogsResult {
			logs,
			total_size : total_count.unwrap()
		})
	}

	async fn _log(&self, request : LogRequest) -> Result<(), Box<dyn error::Error>> {
		let db = self.set_con().await?;
		let mut log: Log = request.log.map(Into::into).unwrap();

		let total_count: Option<i64> = db
			.query("RETURN count(SELECT * FROM logs)")
			.await?
			.take(0)?;

		log.id = Thing {
			tb : "logs".to_string(),
			id : Id::Number(total_count.unwrap())
		};
		
		let _a: Vec<Record> = db
			.create("logs")
			.content(log)
			.await?;

		Ok(())
	}
}
