
-- 01-create-role.sql
-- Creates or updates the login role 'postgres1' with the password you want.
-- Must run as superuser (Docker entrypoint runs as POSTGRES_USER, which is superuser on brand-new clusters).

DO
$$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'postgres1') THEN
        CREATE ROLE postgres1 LOGIN PASSWORD 'postgres123' SUPERUSER;
    ELSE
        ALTER ROLE postgres1 WITH LOGIN PASSWORD 'postgres123';
    END IF;
END
$$ LANGUAGE plpgsql;