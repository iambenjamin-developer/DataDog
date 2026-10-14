#!/bin/sh
# Crea el usuario de solo monitoreo que usa el check de Postgres del Datadog Agent.
# Se ejecuta una única vez, cuando el volumen de datos está vacío.
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE USER datadog WITH PASSWORD '${DD_POSTGRES_PASSWORD}';
    GRANT pg_monitor TO datadog;
    GRANT SELECT ON pg_stat_database TO datadog;
EOSQL
