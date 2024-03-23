DO $$
DECLARE
    i INTEGER;
    version TEXT;
    descriptions JSON;
    create_date TIMESTAMP;
    genForRu TEXT := 'Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Velit scelerisque in dictum non consectetur a erat nam. Imperdiet proin fermentum leo vel orci porta non pulvinar. Est sit amet facilisis magna etiam tempor. Eget nullam non nisi est. Tellus rutrum tellus pellentesque eu tincidunt tortor aliquam nulla. Dignissim convallis aenean et tortor. Fermentum posuere urna nec tincidunt praesent semper. Sed velit dignissim sodales ut eu sem integer vitae. Gravida neque convallis a cras semper auctor. A iaculis at erat pellentesque adipiscing commodo.';
    genForEn TEXT := 'Tincidunt lobortis feugiat vivamus at. Sapien faucibus et molestie ac feugiat. Ac tortor vitae purus faucibus ornare. At erat pellentesque adipiscing commodo elit at imperdiet dui. Laoreet non curabitur gravida arcu ac tortor dignissim convallis. Maecenas ultricies mi eget mauris pharetra et. Ultrices neque ornare aenean euismod elementum nisi. Justo donec enim diam vulputate. Turpis egestas sed tempus urna et pharetra pharetra massa massa. A iaculis at erat pellentesque adipiscing commodo elit at. Tellus orci ac auctor augue mauris augue. Ultrices dui sapien eget mi. Orci ac auctor augue mauris augue. Ut pharetra sit amet aliquam id. Sit amet venenatis urna cursus eget nunc scelerisque viverra. Sapien pellentesque habitant morbi tristique senectus et netus. Fames ac turpis egestas integer.';
BEGIN
    FOR i IN 0..99 LOOP
        version := i || '.' || i || '.' || i || ' JOPA';
        descriptions := json_build_object('ru', genForRu, 'en', genForEn);
        create_date := clock_timestamp();
        INSERT INTO changelogs (version, descriptions, create_date) VALUES (version, descriptions, create_date);
    END LOOP;
END $$;