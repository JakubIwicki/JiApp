#!/usr/bin/env bash
set -euo pipefail

# ── Colors ──────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DATA_DIR="$SCRIPT_DIR/.data"
LOG_DIR="$SCRIPT_DIR/.logs"
PID_DIR="$SCRIPT_DIR/.pids"

mkdir -p "$DATA_DIR" "$LOG_DIR" "$PID_DIR"

# ── Banner ──────────────────────────────────────────────
echo -e "${CYAN}${BOLD}╭──────────────────────────────────────────╮${NC}"
echo -e "${CYAN}${BOLD}│${NC}     ${BOLD}JiApp API — Dev Mode (SQLite)${NC}        ${CYAN}${BOLD}│${NC}"
echo -e "${CYAN}${BOLD}╰──────────────────────────────────────────╯${NC}"
echo ""

# ── JWT key for local dev ───────────────────────────────
export Jwt__Key="${JWT_KEY:-dev-key-at-least-32-chars-long!!}"
export Jwt__Issuer="${JWT_ISSUER:-JiApp-Identity}"
export Jwt__Audience="${JWT_AUDIENCE:-jiapp-gateway}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export Youtube__ApiKey="${YOUTUBE_API_KEY:-placeholder}"

# ── Service definitions ─────────────────────────────────
# format: name|port|project_path|connection_string|protocol
# Dev mode: all services use HTTPS with self-signed cert at ../../certs/dev-cert.pfx
SERVICES=(
  "Identity|5001|src/JiApp.Identity|Data Source=$DATA_DIR/identity_dev.db|https"
  "YtDownloader|5002|src/JiApp.YtDownloader|Data Source=$DATA_DIR/ytdownloader_dev.db|https"
  "ImageTools|5003|src/JiApp.ImageTools||https"
  "Scheduler|5004|src/JiApp.Scheduler|Data Source=$DATA_DIR/scheduler_dev.db|https"
  "Gateway|5000|src/JiApp.Gateway||https"
)

# ── Kill existing processes on our ports ─────────────────
echo -e "${YELLOW}Cleaning up existing processes...${NC}"
for svc in "${SERVICES[@]}"; do
  IFS='|' read -r name port path conn <<< "$svc"
  pid=$(lsof -ti ":$port" 2>/dev/null || true)
  if [ -n "$pid" ]; then
    kill "$pid" 2>/dev/null || true
    echo "  Killed PID $pid on port $port ($name)"
  fi
done
sleep 1

# ── Start each service ───────────────────────────────────
for svc in "${SERVICES[@]}"; do
  IFS='|' read -r name port path conn proto <<< "$svc"

  if [ -n "$conn" ]; then
    export ConnectionString="$conn"
  fi

  echo -e "${GREEN}Starting $name on port $port...${NC}"
  nohup dotnet run --project "$SCRIPT_DIR/$path" \
    --urls "${proto}://0.0.0.0:$port" \
    > "$LOG_DIR/${name,,}.log" 2>&1 &

  echo $! > "$PID_DIR/${name,,}.pid"
  echo "  PID $(cat "$PID_DIR/${name,,}.pid") | Log: $LOG_DIR/${name,,}.log"
done

# ── Wait for Gateway to be ready ─────────────────────────
echo ""
echo -ne "${YELLOW}Waiting for Gateway${NC}"
for i in $(seq 1 30); do
  if curl -sk "https://localhost:5000/health" > /dev/null 2>&1; then
    echo -e " ${GREEN}ready${NC}"
    break
  fi
  echo -n "."
  sleep 1
done

# ── Summary ──────────────────────────────────────────────
echo ""
echo -e "${GREEN}${BOLD}All services started.${NC}"
echo ""
echo "  Gateway:    https://localhost:5000"
echo "  Health:     https://localhost:5000/health"
echo "  Dashboard:  https://localhost:5000/health/dashboard"
echo ""
echo "  Logs:       $LOG_DIR/"
echo "  Stop:       ./stop-dev.sh"
echo ""
