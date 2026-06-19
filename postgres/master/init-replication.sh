#!/bin/bash
set -e

# Create replicator user
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE ROLE replicator WITH REPLICATION LOGIN PASSWORD '${POSTGRES_PASSWORD}';
EOSQL

# Allow replication connections from replica container
echo "host replication replicator all md5" >> "$PGDATA/pg_hba.conf"
# Reload config
psql -U postgres -c "SELECT pg_reload_conf();"
