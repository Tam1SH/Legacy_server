use serde::{Serialize, Deserialize, Deserializer};

#[derive(Serialize, Deserialize, Clone, PartialEq, Debug)]
pub enum UserRole {
	User,
	Helper,
	Admin
}

#[derive(Serialize, Deserialize, Clone, Debug)]
pub struct User {
	
	#[serde(deserialize_with="from_str")]
	pub id: i64,

	pub login: String,

	#[serde(alias = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")]
	pub email : String,
	
	#[serde(alias = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")]
	pub roles: Vec<UserRole>

}

use std::str::FromStr;

fn from_str<'de, D>(deserializer: D) -> Result<i64, D::Error>
where
    D: Deserializer<'de>,
{
    let s = String::deserialize(deserializer)?;
    i64::from_str(&s).map_err(serde::de::Error::custom)
}

