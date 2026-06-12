FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

COPY mssql-entrypoint.sh /usr/local/bin/mssql-entrypoint.sh
COPY create-login.sql /docker-entrypoint-initdb.d/01-services-schema.sql
COPY create-audit.sql /docker-entrypoint-initdb.d/02-audit-schema.sql

RUN chmod +x /usr/local/bin/mssql-entrypoint.sh

USER mssql

ENTRYPOINT ["/usr/local/bin/mssql-entrypoint.sh"]
