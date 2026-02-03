
-- 02-create-db.sql
-- CREATE DATABASE cannot run inside a transaction, so we use \gexec to run the
-- statement only if the DB does not already exist.

\set dbname dBanking_CMS
\set dbowner postgres1

SELECT format('CREATE DATABASE "%s" OWNER %I', :'dbname', :'dbowner')
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = :'dbname') \gexec

-- Optionally, ensure ownership if DB already existed (runs in a tx; OK):
DO
$$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_database WHERE datname = current_setting('dbname', true)) THEN
        -- Quote the name to be safe (already quoted above for create)
        PERFORM 1; -- no-op; ALTER DATABASE OWNER requires literal name, so:
    END IF;
END
$$ LANGUAGE plpgsql;

-- Better: run ALTER unconditionally with IF EXISTS via DO or guard:
DO
$$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_database WHERE datname = 'dBanking_CMS') THEN
        EXECUTE 'ALTER DATABASE "dBanking_CMS" OWNER TO postgres1';
    END IF;
END
$$ LANGUAGE plpgsql;