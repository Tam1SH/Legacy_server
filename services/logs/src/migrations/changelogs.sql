CREATE TABLE IF NOT EXISTS changelogs (
    version VARCHAR(255) NOT NULL,
    descriptions JSON,
    create_date TIMESTAMP,
    PRIMARY KEY (version)
);