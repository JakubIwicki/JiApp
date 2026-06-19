#!/bin/bash
# JiApp Logs — view application and system logs on EC2.
# Usage: ./logs.sh [app|build|watchdog|system] [ec2-instance-id]
set -euo pipefail

REGION="${AWS_REGION:-eu-central-1}"
INSTANCE_ID="${2:-${EC2_INSTANCE_ID:-}}"
if [ -z "$INSTANCE_ID" ]; then echo "Usage: $0 [app|build|watchdog|system|compose] <instance-id>  (or set EC2_INSTANCE_ID)" >&2; exit 1; fi
TYPE="${1:-app}"

case "$TYPE" in
    app)     LOG_PATH="/opt/jiapp/logs/gateway-log-*.txt"; TAIL="30" ;;
    build)   LOG_PATH="/opt/jiapp/logs/build.log"; TAIL="30" ;;
    watchdog) LOG_PATH="/opt/jiapp/logs/watchdog.log"; TAIL="20" ;;
    system)  LOG_PATH="/opt/jiapp/logs/system.log"; TAIL="30" ;;
    compose) LOG_PATH=""; DOCKER_CMD="docker compose -f /opt/jiapp/docker-compose.yml -f /opt/jiapp/docker-compose.prod.yml logs --tail 50 2>/dev/null || echo no-compose-logs" ;;
    *)
        echo "Usage: ./logs.sh [app|build|watchdog|system|compose] [instance-id]"
        exit 1
        ;;
esac

echo "==> JiApp logs: $TYPE ($INSTANCE_ID)"

if [ -n "${DOCKER_CMD:-}" ]; then
    CMD_ID=$(aws ssm send-command --region "$REGION" --instance-ids "$INSTANCE_ID" \
        --document-name "AWS-RunShellScript" \
        --parameters "commands=[\"${DOCKER_CMD}\"]" \
        --query "Command.CommandId" --output text)
else
    CMD_ID=$(aws ssm send-command --region "$REGION" --instance-ids "$INSTANCE_ID" \
        --document-name "AWS-RunShellScript" \
        --parameters "commands=[\"tail -${TAIL} ${LOG_PATH} 2>/dev/null || echo no-log-file\"]" \
        --query "Command.CommandId" --output text)
fi

sleep 8
aws ssm get-command-invocation --region "$REGION" --command-id "$CMD_ID" \
    --instance-id "$INSTANCE_ID" --query "StandardOutputContent" --output text 2>/dev/null
