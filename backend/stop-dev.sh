#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PID_DIR="$SCRIPT_DIR/.pids"

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

echo "Stopping JiApp services..."

if [ ! -d "$PID_DIR" ]; then
  echo "No PID directory found. Killing by port instead..."
  for port in 5000 5001 5002 5003 5004; do
    pid=$(lsof -ti ":$port" 2>/dev/null || true)
    if [ -n "$pid" ]; then
      kill "$pid" 2>/dev/null || true
      echo "  Killed PID $pid (port $port)"
    fi
  done
  exit 0
fi

for pidfile in "$PID_DIR"/*.pid; do
  if [ -f "$pidfile" ]; then
    name=$(basename "$pidfile" .pid)
    pid=$(cat "$pidfile")
    if kill "$pid" 2>/dev/null; then
      echo -e "  ${GREEN}✓${NC} $name (PID $pid)"
    else
      echo -e "  ${RED}✗${NC} $name (PID $pid not running)"
    fi
    rm -f "$pidfile"
  fi
done

# Clean up any remaining processes on our ports
for port in 5000 5001 5002 5003 5004; do
  pid=$(lsof -ti ":$port" 2>/dev/null || true)
  if [ -n "$pid" ]; then
    kill "$pid" 2>/dev/null || true
  fi
done

echo "Done."
