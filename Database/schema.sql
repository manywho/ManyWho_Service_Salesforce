CREATE TABLE storedjson
(
  id character varying(255) NOT NULL,
  json jsonb NOT NULL,
  createdat timestamp with time zone NOT NULL DEFAULT now(),
  CONSTRAINT pk_storedjson PRIMARY KEY (id)
);
