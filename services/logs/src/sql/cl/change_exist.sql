BEGIN;

DELETE FROM changelogs WHERE version = $1;

INSERT INTO changelogs (version, descriptions, create_date) 
	VALUES ($1, json_build_object('ru', $2, 'en', $3), $4);

COMMIT;