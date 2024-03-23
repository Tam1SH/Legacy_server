use tonic::transport::Channel;
use crate::error::Error;
use crate::grpc::changelogs::generated::changelogs_client::ChangelogsClient;


//HACK я не знаю что с этим можно пока сделать, но пускай будет так.
const URL: &str = if cfg!(test) {
	dotenv!("GRPC_MAIN_MOCK")
}
else {
	dotenv!("GRPC_MAIN")
};

pub async fn get_client() -> Result<ChangelogsClient<Channel>, Error> {
	Ok(ChangelogsClient::connect(URL).await?)

}

