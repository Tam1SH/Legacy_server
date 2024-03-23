use tonic::transport::Server;
use crate::{grpc::api::logs_service_server::LogsServiceServer, grpc::server::Logs};

pub async fn config_grpc_server() -> Result<(), tonic::transport::Error> {
	Server::builder()
        .add_service(LogsServiceServer::new(Logs::default()))
        .serve(dotenv!("HOST_RPC").parse().unwrap())
		.await
}