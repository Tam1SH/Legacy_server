use actix_web::{Scope, web};
use super::controller::*;


pub fn changelog_controller() -> Scope {
	
	web::scope("/changelogs")
		.service(get_changelogs)
		.service(get_changelogs_count)
		.service(upload_changelog)
		.service(change_exist_update)
		.service(get_changelog_by_version)
}