CREATE TABLE item (
	id varchar(300) NOT NULL,
	user_id varchar(50) NOT NULL,
	date_modified timestamptz NOT NULL,
	CONSTRAINT item_pk PRIMARY KEY (id)
);

CREATE TABLE public.item_property (
	id serial NOT NULL,
	item_id varchar(300) NOT NULL,
	"name" varchar(100) NOT NULL,
	value varchar(1000) NOT NULL,
	CONSTRAINT item_property_pk PRIMARY KEY (id),
	CONSTRAINT item_property_fk FOREIGN KEY (item_id) REFERENCES item(id) ON DELETE CASCADE ON UPDATE CASCADE
);
CREATE INDEX item_property_name_idx ON public.item_property ("name");

CREATE TABLE item_content (
	item_id varchar(300) NOT NULL,
	mime_type varchar(200) NOT NULL,
	hash int8 NOT NULL,
	size int8 NOT NULL,
	content BYTEA NULL,
	user_id varchar(50) NOT NULL,
	date_modified timestamptz NOT NULL,
	CONSTRAINT item_content_pk PRIMARY KEY (item_id),
	CONSTRAINT item_content_fk FOREIGN KEY (item_id) REFERENCES item(id) ON DELETE CASCADE ON UPDATE CASCADE
);
