DO
$$
BEGIN
   IF NOT EXISTS (
      SELECT FROM pg_catalog.pg_roles WHERE rolname = 'pharma_user_1'
   ) THEN
      CREATE ROLE pharma_user_1 LOGIN PASSWORD 'sh_pharma_user_2026_1';
   END IF;
END
$$;

SELECT 'CREATE DATABASE pharmacydb OWNER pharma_user_1'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'pharmacydb')\gexec

GRANT ALL PRIVILEGES ON DATABASE pharmacydb TO pharma_user_1;