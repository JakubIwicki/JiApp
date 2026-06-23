#!/bin/bash
# JiApp stop watchdog — checks idle for 20 consecutive minutes, then
# backs up databases, gracefully stops containers, and stops the EC2 instance.
# Runs every 60 seconds via systemd timer.
set -euo pipefail

exec 200>/tmp/jiapp_watchdog.lock
flock -n 200 || exit 0

# Fetch IMDSv2 token; falls back to IMDSv1 if token endpoint unreachable
TOKEN=$(curl -s -X PUT "http://169.254.169.254/latest/api/token" -H "X-aws-ec2-metadata-token-ttl-seconds: 300" 2>/dev/null || true)
IMDS_HDR=()
if [ -n "${TOKEN:-}" ]; then
    IMDS_HDR=(-H "X-aws-ec2-metadata-token: $TOKEN")
fi
INSTANCE_ID=$(curl -s "${IMDS_HDR[@]}" http://169.254.169.254/latest/meta-data/instance-id 2>/dev/null || true)
REGION=$(curl -s "${IMDS_HDR[@]}" http://169.254.169.254/latest/meta-data/placement/region 2>/dev/null || true)
IDLE_FILE="/tmp/jiapp_idle_count"
MAX_IDLE=20  # 20 consecutive checks × 60s = 20 minutes
LOG_FILE="/opt/jiapp/logs/watchdog.log"

mkdir -p "$(dirname "$LOG_FILE")"

# ── Idle check ───────────────────────────────────────────

check_idle() {
    # No yt-dlp or ffmpeg processes running
    local procs
    procs=$(pgrep -c 'yt-dlp|ffmpeg' 2>/dev/null) || true
    procs=${procs:-0}

    # No established TCP connections to Gateway port
    local conns
    conns=$(ss -tn state established sport = :6700 2>/dev/null | tail -n +2 | wc -l)

    [ "$procs" -eq 0 ] && [ "$conns" -eq 0 ]
}

# ── Idle counter ─────────────────────────────────────────

if check_idle; then
    COUNT=$(($(cat "$IDLE_FILE" 2>/dev/null || echo 0) + 1))
else
    COUNT=0
fi
echo "$COUNT" > "$IDLE_FILE"

# Log status
PROCS=$(pgrep -c 'yt-dlp|ffmpeg' 2>/dev/null) || true
PROCS=${PROCS:-0}
CONNS=$(ss -tn state established sport = :6700 2>/dev/null | tail -n +2 | wc -l) || true
CONNS=${CONNS:-0}
echo "[$(date)] Idle ${COUNT}/${MAX_IDLE}  yt-dlp=${PROCS} conns=${CONNS}" >> "$LOG_FILE"

# ── Threshold reached ────────────────────────────────────

if [ "$COUNT" -ge "$MAX_IDLE" ]; then
    echo "[$(date)] IDLE THRESHOLD — initiating shutdown" >> "$LOG_FILE"

    # Safety: verify we can resolve instance identity before tearing anything down
    if [ -z "${INSTANCE_ID:-}" ] || [ -z "${REGION:-}" ]; then
        echo "[$(date)] ABORT: could not resolve INSTANCE_ID/REGION from IMDS — skipping shutdown to avoid stopping containers without stopping the instance" >> "$LOG_FILE"
        exit 0
    fi

    # 1. Backup databases
    echo "[$(date)] Step 1/3: Backup databases..." >> "$LOG_FILE"
    /opt/jiapp/backup.sh >> "$LOG_FILE" 2>&1 || echo "[$(date)] Backup had errors — continuing" >> "$LOG_FILE"

    # 2. Graceful Docker Compose stop
    echo "[$(date)] Step 2/3: Stopping Docker Compose..." >> "$LOG_FILE"
    cd /opt/jiapp
    docker compose -f docker-compose.yml -f docker-compose.prod.yml stop --timeout 30 >> "$LOG_FILE" 2>&1

    # 3. Stop EC2 instance
    echo "[$(date)] Step 3/3: Stopping EC2 instance ${INSTANCE_ID}..." >> "$LOG_FILE"
    aws ec2 stop-instances --region "$REGION" --instance-ids "$INSTANCE_ID" >> "$LOG_FILE" 2>&1

    rm -f "$IDLE_FILE"
    echo "[$(date)] Shutdown complete" >> "$LOG_FILE"
fi
