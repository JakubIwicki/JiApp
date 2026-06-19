#!/bin/bash
# JiApp database backup — compresses .db files and uploads to S3.
# Triggered: before auto-stop and via systemd timer (daily).
set -euo pipefail

ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
BUCKET="jiapp-backups-${ACCOUNT_ID}"
TIMESTAMP=$(date -u +%Y-%m-%dT%H:%M:%SZ)
DATA_DIR="/opt/jiapp/data"
UPLOADED=0

echo "[$(date)] Starting backup to s3://${BUCKET}/"

for db in "${DATA_DIR}"/*.db; do
    if [ ! -f "$db" ]; then
        echo "[$(date)] No databases found in ${DATA_DIR}"
        exit 0
    fi

    name=$(basename "$db" .db)
    key="db-backups/${name}/${TIMESTAMP}.db.gz"

    # Compress and stream to S3
    if gzip -c "$db" | aws s3 cp - "s3://${BUCKET}/${key}" --no-progress; then
        echo "[$(date)] Backed up ${name} → s3://${BUCKET}/${key}"
        UPLOADED=$((UPLOADED + 1))
    else
        echo "[$(date)] ERROR: failed to backup ${name}" >&2
    fi
done

echo "[$(date)] Backup complete — ${UPLOADED} databases"
