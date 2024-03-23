use jsonwebtoken::{decode, Algorithm, DecodingKey, Validation};
use crate::{error::Error, model::user::User};

pub fn decode_token<T: Into<String>>(token: T, secret: &[u8]) -> Result<User, Error> {

	let mut validation = Validation::new(Algorithm::HS256);
	validation.set_audience(&[dotenv!("JWT_AUDIENCE")]);
	validation.set_issuer(&[dotenv!("JWT_ISSUER")]);
	
	validation.validate_nbf = false;
	validation.validate_exp = false;

	Ok(
		decode::<User>(
        &token.into(),
        &DecodingKey::from_secret(secret),
        &validation,
		)?.claims
	)

}