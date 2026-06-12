#!/bin/bash
set -e

SQLCMD=/opt/mssql-tools18/bin/sqlcmd

# Start SQL Server in the background
/opt/mssql/bin/sqlservr &
SQLPID=$!

echo "[db-init] Waiting for SQL Server to accept connections..."
for i in $(seq 1 60); do
    if $SQLCMD -S localhost -U sa -P "$SA_PASSWORD" -Q "SELECT 1" -b -o /dev/null -C 2>/dev/null; then
        echo "[db-init] SQL Server is ready (attempt $i)."
        break
    fi
    if [ "$i" -eq 60 ]; then
        echo "[db-init] ERROR: SQL Server did not start within 120 seconds."
        exit 1
    fi
    sleep 2
done

# Only run init scripts if LifeSci360_Services does not exist yet.
# This makes restarts safe — the volume already has the schema on subsequent runs.
DB_COUNT=$($SQLCMD -S localhost -U sa -P "$SA_PASSWORD" \
    -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name='LifeSci360_Services'" \
    -h -1 -C 2>/dev/null | tr -d ' \r\n')

if [ "$DB_COUNT" = "0" ]; then
    echo "[db-init] First-time setup — running initialization scripts..."
    for f in $(ls /docker-entrypoint-initdb.d/*.sql | sort); do
        echo "[db-init]   -> $f"
        $SQLCMD -S localhost -U sa -P "$SA_PASSWORD" -i "$f" -C
    done
    echo "[db-init] Schema initialization complete."
else
    echo "[db-init] Databases already exist — skipping initialization."
fi

echo "[db-init] Handing off to SQL Server process (PID $SQLPID)..."
wait $SQLPID
