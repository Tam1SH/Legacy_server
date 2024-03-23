use std::cell::Ref;

use actix_web::{cookie::Cookie, HttpRequest};

pub fn make_lang(lang: &str) -> String {
    format!("c={}|uic={}", lang, lang)
}


pub fn parse_lang(req: &HttpRequest) -> Option<String> {

	match req.cookies() {
		Ok(cookies) => {
			parse_lang_from_cookie(cookies)
				.map(|parsed_lang| parsed_lang.0)

		},
		Err(_) => None
	}
}

pub fn parse_lang_from_cookie(
	cookies: Ref<'_, Vec<Cookie<'static>>>
) -> Option<(String, String)> {

	let cookie = cookies
		.iter()
		.map(|c| (c.name(), c.value()))
		.find(|c| c.0 == "lang")
		.take()
		.map(|c| c.1.to_string());

	_parse_lang(cookie)
		.and_then(|(opt1, opt2)| {
			if let (Some(s1), Some(s2)) = (opt1, opt2) {
				Some((s1, s2))
			} else {
				None
			}
		})
}

fn _parse_lang(
	input: Option<String>
) -> Option<(Option<String>, Option<String>)> {
    if let Some(input_str) = input {
        let regex = regex::Regex::new(r"c=([^|]+)\|uic=([^|]+)").unwrap();
        if let Some(captures) = regex.captures(&input_str) {
            return Some((
				captures.get(1).map(|c| c.as_str().to_string()), 
				captures.get(2).map(|c| c.as_str().to_string())
			));
        }
    }
    None
}