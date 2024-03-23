use std::collections::HashMap;

use chrono::{DateTime, Utc};
use serde::{Serialize, Deserialize};
#[derive(Debug, Serialize, Deserialize, Clone, PartialEq)]
pub struct Changelog {
	pub version: String,
	pub create_date: DateTime<Utc>,
	pub descriptions: HashMap<String, String>
}
