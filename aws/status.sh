#!/bin/bash
# JiApp Status — check EC2 state, container health, and recent logs.
# Usage: ./status.sh [ec2-instance-id]
set -euo pipefail

REGION="${AWS_REGION:-eu-central-1}"
INSTANCE_ID="${1:-${EC2_INSTANCE_ID:-}}"
if [ -z "$INSTANCE_ID" ]; then echo "Usage: $0 <instance-id>  (or set EC2_INSTANCE_ID)" >&2; exit 1; fi

echo "═══════════════════════════════════════════"
echo " JiApp Status"
echo "═══════════════════════════════════════════"

# ── EC2 state ──
STATE=$(aws ec2 describe-instances --region "$REGION" --instance-ids "$INSTANCE_ID" \
    --query "Reservations[0].Instances[0].State.Name" --output text 2>/dev/null || echo "unknown")
EIP=$(aws ec2 describe-instances --region "$REGION" --instance-ids "$INSTANCE_ID" \
    --query "Reservations[0].Instances[0].PublicIpAddress" --output text 2>/dev/null || echo "none")

echo "  EC2:       $INSTANCE_ID ($STATE)"
echo "  Public IP: $EIP"

# ── SSM reachable? ──
SSM_STATUS=$(aws ssm describe-instance-information --region "$REGION" \
    --filters "Key=InstanceIds,Values=$INSTANCE_ID" \
    --query "InstanceInformationList[0].PingStatus" --output text 2>/dev/null || echo "offline")
echo "  SSM:       $SSM_STATUS"

if [ "$SSM_STATUS" != "Online" ]; then
    echo ""
    echo "  Instance is not reachable via SSM."
    echo "  Wake it up: curl -X POST ${API_GATEWAY_URL:-https://<your-api-gateway-id>.execute-api.<region>.amazonaws.com}/start"
    exit 0
fi

# ── Container status ──
echo ""
echo "  Containers:"
CMD_ID=$(aws ssm send-command --region "$REGION" --instance-ids "$INSTANCE_ID" \
    --document-name "AWS-RunShellScript" \
    --parameters 'commands=["docker ps --format \"table {{.Names}}\t{{.Status}}\t{{.Ports}}\" 2>/dev/null || echo no-containers"]' \
    --query "Command.CommandId" --output text)

sleep 6
aws ssm get-command-invocation --region "$REGION" --command-id "$CMD_ID" \
    --instance-id "$INSTANCE_ID" --query "StandardOutputContent" --output text 2>/dev/null | \
    while IFS= read -r line; do echo "    $line"; done

# ── Health check ──
echo ""
echo "  Health check:"
if [ -n "$EIP" ] && [ "$EIP" != "none" ]; then
    HEALTH=$(curl -sk "https://${EIP}:6700/health" 2>/dev/null || echo "unreachable")
    echo "    $HEALTH"
fi

# ── Recent logs ──
echo ""
echo "  Recent activity (watchdog):"
CMD_ID=$(aws ssm send-command --region "$REGION" --instance-ids "$INSTANCE_ID" \
    --document-name "AWS-RunShellScript" \
    --parameters 'commands=["tail -5 /opt/jiapp/logs/watchdog.log 2>/dev/null || echo no-watchdog-log"]' \
    --query "Command.CommandId" --output text)

sleep 6
aws ssm get-command-invocation --region "$REGION" --command-id "$CMD_ID" \
    --instance-id "$INSTANCE_ID" --query "StandardOutputContent" --output text 2>/dev/null | \
    while IFS= read -r line; do echo "    $line"; done
