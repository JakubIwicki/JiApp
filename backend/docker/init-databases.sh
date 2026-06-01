#!/bin/bash
# Creates multiple PostgreSQL databases on first container startup.
# Mounted into /docker-entrypoint-initdb.d/ in the postgres container.

set -e

for db in ${POSTGRES_MULTIPLE_DATABASES//,/ }; do
  echo "Creating database: $db"
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
    CREATE DATABASE "$db";
EOSQL
done
