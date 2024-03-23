use chrono::NaiveDateTime;

use crate::model::changelogs::Changelog;


pub struct ChangelogDb {
	pub version: String,
	pub create_date: Option<NaiveDateTime>,
	pub descriptions: serde_json::Value,
}

impl From<ChangelogDb> for Changelog {
	fn from(value: ChangelogDb) -> Self {
		Changelog {
			create_date: value.create_date.unwrap().and_utc(),
			descriptions : serde_json::from_value(value.descriptions).unwrap(),
			version : value.version
		}
	}
}