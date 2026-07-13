#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Source AWS identifiers
if [[ -f "$SCRIPT_DIR/.env" ]]; then
  source "$SCRIPT_DIR/.env"
else
  echo "ERROR: aws/.env not found at $SCRIPT_DIR/.env" >&2
  exit 1
fi

export PATH="$HOME/.local/bin:$PATH"

# Verify required vars
: "${EC2_INSTANCE_ID:?EC2_INSTANCE_ID not set in aws/.env}"
: "${AWS_REGION:?AWS_REGION not set in aws/.env}"

echo "Starting SSM tunnels for sqlite-web on $EC2_INSTANCE_ID ..."

PORTS=(8081 8082 8083 8084)
LABELS=("identity    " "scheduler   " "ytdownloader" "lovingboards")

cleanup() {
  echo ""
  echo "Stopping tunnels..."
  for pid in "${PIDS[@]:-}"; do
    kill "$pid" 2>/dev/null || true
  done
  echo "All tunnels stopped."
}
trap cleanup EXIT

PIDS=()
for i in "${!PORTS[@]}"; do
  port="${PORTS[$i]}"
  label="${LABELS[$i]}"

  aws ssm start-session \
    --region "$AWS_REGION" \
    --target "$EC2_INSTANCE_ID" \
    --document-name "AWS-StartPortForwardingSession" \
    --parameters "{\"portNumber\":[\"$port\"],\"localPortNumber\":[\"$port\"]}" &
  PIDS+=($!)

  echo "  http://localhost:$port  →  $label"
  sleep 2
done

echo ""
echo "Tunnels active. Press Ctrl+C to stop."

# Block until interrupted
wait
