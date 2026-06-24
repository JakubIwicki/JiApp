#!/bin/bash
# JiApp health watchdog — checks Gateway health every 60s via systemd timer;
# restarts the stack if the gateway is down, with guards so it never fights
# a teardown (stop-watchdog) or a deploy/build.
set -uo pipefail

exec 201>/tmp/jiapp_health.lock
flock -n 201 || exit 0

LOG_FILE=/opt/jiapp/logs/health.log
mkdir -p "$(dirname "$LOG_FILE")"

# Stand down if a deploy or build is in progress
if [ -f /tmp/jiapp_deploying ]; then
    exit 0
fi

# Stand down if a teardown is in progress (but not if the marker is stale/leaked)
if [ -f /tmp/jiapp_stopping ]; then
    if [ -z "$(find /tmp/jiapp_stopping -mmin +3 2>/dev/null)" ]; then
        exit 0
    fi
fi

# Health check: if Gateway is healthy, nothing to do
if curl -sk --max-time 8 https://localhost:6700/health 2>/dev/null | grep -qi healthy; then
    exit 0
fi

# Gateway is down — restart the stack
echo "[$(date)] Gateway unhealthy — restarting stack" >> "$LOG_FILE"
/opt/jiapp/startup.sh >> "$LOG_FILE" 2>&1 || echo "[$(date)] startup.sh returned non-zero" >> "$LOG_FILE"
echo "[$(date)] Health recovery complete" >> "$LOG_FILE"
