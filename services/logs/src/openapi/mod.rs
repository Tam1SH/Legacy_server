use std::{fs::File, io::Write};

use utoipa::OpenApi;

use crate::{api::changelogs::{
	requests_types::*, 
	response_types::*,
	controller::*
}, error::ResponseError};


pub fn openapi() -> Result<(), Box<dyn std::error::Error>> {
	#[derive(OpenApi)]
	#[openapi(
		paths(
			get_changelogs, 
			get_changelogs_count,
			upload_changelog,
			change_exist_update,
			get_changelog_by_version
		),
		components(
			schemas(
				GetChangeLogsModel, 
				GetChangeLogsResult,
				GetChangeLogModel,
				ChangeLogUploadModel,
				ResponseError
			)
		)
	)]
	struct ApiDoc;

	
    let mut file = File::create(format!("{}/openapi.g/openapi.yaml", std::env::current_dir().unwrap().to_str().unwrap()))?;

	let open_api = ApiDoc::openapi().to_yaml().unwrap();
    file.write_all(open_api.as_bytes())?;


	Ok(())

	

}
