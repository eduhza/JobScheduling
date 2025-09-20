# COMO É UM ARQUIVO LINUX PRECISA SER SALVO COMO LF (LINE FEED) E NÃO CRLF (CARRIAGE RETURN LINE FEED)
#!/bin/bash
set -e

# A variável POSTGRES_MULTIPLE_DATABASES é fornecida pelo docker-compose
if [ -n "$POSTGRES_MULTIPLE_DATABASES" ]; then
  echo "Multiple database creation requested: $POSTGRES_MULTIPLE_DATABASES"
  for db in $(echo $POSTGRES_MULTIPLE_DATABASES | tr ',' ' '); do
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
      CREATE DATABASE "$db";
EOSQL
  done
  echo "Multiple databases created"
fi