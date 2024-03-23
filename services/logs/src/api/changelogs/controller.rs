


use actix_multipart::form::{MultipartForm, text::Text};
use actix_web::{post, web::{Json, Data}, HttpRequest, get, HttpResponse};
use crate::{thirdparty::auth::RequireAuth, api::changelogs::requests_types::{GetChangeLogModel, ChangeLogUploadModel}, model::changelogs::Changelog, error::Error, actix_config::AppState};
use crate::model::user::UserRole;
use crate::{
	error::{ResponseError, ActixResult}, 
	api::changelogs::response_types::GetChangeLogsResult, 
	utils::parse_lang::parse_lang, 
	pool::get_connections
};
use super::{requests_types::GetChangeLogsModel, service};

use service::*;

#[utoipa::path(
	context_path = "/api/changelogs",
	tag="Changelogs",
	request_body = GetChangeLogsModel,
	responses(
		(status = 200, body = Vec<GetChangeLogsResult>),
		(status = 500, body = ResponseError)
	)
)]
#[post("/getChangeLogs")]
async fn get_changelogs(
	req: HttpRequest,
	model: Json<GetChangeLogsModel>,
    state: Data<AppState>
) -> ActixResult<Json<Vec<GetChangeLogsResult>>, ResponseError>  {

	let lang = parse_lang(&req);
	let (db, mut cache) = get_connections(&state.pools).await?;
	
	let logs = ChangelogService::new(db, &mut cache, &state.config)
		.get_changelogs(model.offset, model.count, lang)
		.await?;
	
	Ok(Json(logs))
}

#[utoipa::path(
	context_path = "/api/changelogs",
	tag="Changelogs",
	responses(
		(status = 200, body = usize),
		(status = 500, body = ResponseError)
	)
)]
#[get("/getChangeLogsCount")]
async fn get_changelogs_count(
    state: Data<AppState>,
) -> ActixResult<String, ResponseError>  {

	let (db, mut cache) = get_connections(&state.pools).await?;
	
	let count = ChangelogService::new(db, &mut cache, &state.config)
		.get_changelogs_count()
		.await?;
	
	Ok(count.to_string())
}

#[utoipa::path(
	context_path = "/api/changelogs",
	tag="Changelogs",
	request_body = GetChangeLogModel,
	responses(
		(status = 200, body = Vec<GetChangeLogsResult>),
		(status = 500, body = ResponseError)
	)
)]
#[get("/getChangelogByVersion", wrap = "RequireAuth::allowed_roles(vec![UserRole::Admin])")]
async fn get_changelog_by_version(
    state: Data<AppState>,
	model: Json<GetChangeLogModel>,
) -> ActixResult<Json<Vec<GetChangeLogsResult>>, ResponseError>  {

	let (db, mut cache) = get_connections(&state.pools).await?;

	Ok(Json(
		ChangelogService::new(db, &mut cache, &state.config)
			.get_changelog_by_version(model.version.to_string())
			.await?
	))
}

#[utoipa::path(
	context_path = "/api/changelogs",
	tag="Changelogs",
	request_body(content = ChangeLogUploadModel, content_type = "multipart/form-data"),
	responses(
		(status = 500, body = ResponseError)
	)
)]
#[post("/uploadUpdate", wrap = "RequireAuth::allowed_roles(vec![UserRole::Admin])")]
async fn upload_changelog(
    state: Data<AppState>,
    MultipartForm(mut form): MultipartForm<ChangeLogUploadModel>,
) -> ActixResult<HttpResponse, ResponseError>  {

	let (db, mut cache) = get_connections(&state.pools).await?;

	let log = Changelog {
		create_date: *form.createDate,
		descriptions : serde_json::from_str(&form.data)
			.map_err(|err| Error::ErrorStr(format!("{}", err)))?,
		version: form.version.to_string()
	};

	let file = form.injector
		.pop()
		.ok_or_else(|| Error::Error("No file provided"))?
		.file
		.into_parts().0;

	ChangelogService::new(db, &mut cache, &state.config)
		.upload_changelog(
			log, 
			form.ChangeInDatabase.unwrap_or(Text(false)).0,
			form.Publish.unwrap_or(Text(false)).0,
			file
		)
		.await?;
	
	Ok(HttpResponse::Ok().finish())
}


#[utoipa::path(
	context_path = "/api/changelogs",
	tag="Changelogs",
	request_body = GetChangeLogModel,
	responses(
		(status = 500, body = ResponseError)
	)
)]
#[post("/changeExistUpdate", wrap = "RequireAuth::allowed_roles(vec![UserRole::Admin])")]
async fn change_exist_update(
    state: Data<AppState>,
    MultipartForm(form): MultipartForm<ChangeLogUploadModel>,
) -> ActixResult<HttpResponse, ResponseError>  {

	let (db, mut cache) = get_connections(&state.pools).await?;

	let log = Changelog {
		create_date: *form.createDate,
		descriptions : serde_json::from_str(&form.data)
			.map_err(|err| Error::ErrorStr(format!("{}", err)))?,
		version: form.version.to_string()
	};

	ChangelogService::new(db, &mut cache, &state.config)
		.change_exist_changelog(log)
		.await?;
	
	Ok(
		HttpResponse::Ok().finish()
	)
}








