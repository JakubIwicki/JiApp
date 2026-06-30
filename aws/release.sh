#!/bin/bash
# JiApp Release — single-command full deploy: wake, sync secrets, upload source,
# build on EC2 (ARM64), pull, start, health-check, and print status.
# Usage: ./aws/release.sh [--no-build] [tag]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

# Source infra identifiers (EC2_INSTANCE_ID, AWS_REGION, etc.)
if [ -f "${REPO_ROOT}/aws/.env" ]; then
    source "${REPO_ROOT}/aws/.env"
fi

REGION="${AWS_REGION:-eu-central-1}"
INSTANCE_ID="${EC2_INSTANCE_ID:-}"
if [ -z "$INSTANCE_ID" ]; then
    echo "Usage: $0 [--no-build] [tag]" >&2
    echo "  EC2_INSTANCE_ID not set. Source aws/.env or export EC2_INSTANCE_ID." >&2
    exit 1
fi

ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
BUCKET="jiapp-deploy-config-${ACCOUNT_ID}"

# ── Parse args ───────────────────────────────────────────────

NO_BUILD=false
while [ $# -gt 0 ]; do
    case "$1" in
        --no-build) NO_BUILD=true; shift ;;
        *) break ;;
    esac
done
TAG="${1:-$(date -u +%Y%m%d-%H%M%S)}"

echo "==> Release: $TAG  (instance: $INSTANCE_ID, region: $REGION)"
if $NO_BUILD; then
    echo "==> --no-build: skipping EC2 build, will use existing images for tag '$TAG'"
fi

# ── 1. Wake the instance ─────────────────────────────────────

echo ""
echo "==> Checking instance state..."

STATE=$(aws ec2 describe-instances --region "$REGION" --instance-ids "$INSTANCE_ID" \
    --query "Reservations[0].Instances[0].State.Name" --output text 2>/dev/null || echo "unknown")
echo "    Instance state: $STATE"

SSM_ONLINE=false
if [ "$STATE" = "running" ]; then
    SSM_STATUS=$(aws ssm describe-instance-information --region "$REGION" \
        --filters "Key=InstanceIds,Values=$INSTANCE_ID" \
        --query "InstanceInformationList[0].PingStatus" --output text 2>/dev/null || echo "offline")
    if [ "$SSM_STATUS" = "Online" ]; then
        echo "    SSM already Online — skipping wake"
        SSM_ONLINE=true
    fi
fi

if ! $SSM_ONLINE; then
    if [ "$STATE" != "running" ]; then
        echo "==> Starting instance..."
        aws ec2 start-instances --instance-ids "$INSTANCE_ID" --region "$REGION" --no-cli-pager
        echo "==> Waiting for instance to reach 'running'..."
        aws ec2 wait instance-running --instance-ids "$INSTANCE_ID" --region "$REGION"
        echo "    Instance is running."
    fi

    echo "==> Waiting for SSM to come Online..."
    for i in $(seq 1 30); do
        sleep 6
        SSM_STATUS=$(aws ssm describe-instance-information --region "$REGION" \
            --filters "Key=InstanceIds,Values=$INSTANCE_ID" \
            --query "InstanceInformationList[0].PingStatus" --output text 2>/dev/null || echo "offline")
        echo "    [${i}/30] SSM: $SSM_STATUS"
        if [ "$SSM_STATUS" = "Online" ]; then
            SSM_ONLINE=true
            break
        fi
    done

    if ! $SSM_ONLINE; then
        echo "ERROR: SSM agent did not come Online after 3 minutes. Aborting." >&2
        exit 1
    fi
fi

# ── 2. Sync prod secrets to S3 ───────────────────────────────

echo ""
echo "==> Syncing prod secrets to S3..."

if [ ! -f "${REPO_ROOT}/aws/.env.prod" ]; then
    echo "ERROR: aws/.env.prod not found." >&2
    echo "  Create aws/.env.prod from aws/.env.prod.example and fill in the prod secrets:" >&2
    echo "    cp aws/.env.prod.example aws/.env.prod" >&2
    exit 1
fi

aws s3 cp "${REPO_ROOT}/aws/.env.prod" "s3://${BUCKET}/ec2/.env" --region "$REGION" --no-cli-pager
echo "    Secrets uploaded to s3://${BUCKET}/ec2/.env"

# ── 3. Upload source + configs ───────────────────────────────

echo ""
echo "==> Uploading source + configs (deploy.sh --source --no-start)..."
"${SCRIPT_DIR}/deploy.sh" --source --no-start "$TAG" "$INSTANCE_ID"

# ── 3b. Refresh EC2 helper scripts so a changed build-and-push.sh takes effect ─

echo ""
echo "==> Refreshing EC2 helper scripts from S3 before build..."
SYNC_CMD="for f in build-and-push.sh startup.sh stop-watchdog.sh backup.sh jiapp-health.sh; do aws s3 cp s3://${BUCKET}/ec2/\$f /opt/jiapp/\$f --region ${REGION} 2>/dev/null || true; done; chmod +x /opt/jiapp/*.sh"
SYNC_ID=$(aws ssm send-command --region "$REGION" --instance-ids "$INSTANCE_ID" \
    --document-name "AWS-RunShellScript" \
    --comment "JiApp refresh helper scripts $TAG" \
    --parameters "commands=[\"${SYNC_CMD}\"]" \
    --query "Command.CommandId" --output text)
echo "    SSM sync command: $SYNC_ID — waiting for it to finish..."
aws ssm wait command-executed --region "$REGION" --command-id "$SYNC_ID" --instance-id "$INSTANCE_ID" 2>/dev/null || true
echo "    EC2 helper scripts refreshed."

# ── 4. Build on EC2 (skip if --no-build) ─────────────────────

if ! $NO_BUILD; then
    echo ""
    echo "==> Building on EC2 (this takes 15-20 min)..."

    BUILD_CMD="cd /opt/jiapp && ./build-and-push.sh ${TAG}"
    CMD_ID=$(aws ssm send-command --region "$REGION" --instance-ids "$INSTANCE_ID" \
        --document-name "AWS-RunShellScript" \
        --comment "JiApp release build $TAG" \
        --parameters "commands=[\"${BUILD_CMD}\"]" \
        --query "Command.CommandId" --output text)

    echo "    SSM Command ID: $CMD_ID"
    echo "    Polling build status (sleep 20s between checks, up to ~30 min)..."

    POLL=0
    MAX_POLLS=90
    LAST_OUT_LEN=0
    LAST_ERR_LEN=0
    FINAL_STATUS=""
    while [ $POLL -lt $MAX_POLLS ]; do
        sleep 20
        POLL=$((POLL + 1))

        # get-command-invocation can transiently fail right after send; tolerate early errors
        INVOCATION=$(aws ssm get-command-invocation --region "$REGION" \
            --command-id "$CMD_ID" --instance-id "$INSTANCE_ID" \
            --query "{Status:Status,Out:StandardOutputContent,Err:StandardErrorContent}" \
            --output json 2>/dev/null || echo "")

        if [ -z "$INVOCATION" ]; then
            echo "    [${POLL}/${MAX_POLLS}] SSM invocation not ready yet, retrying..."
            continue
        fi

        STATUS=$(echo "$INVOCATION" | python3 -c "import sys,json; print(json.load(sys.stdin).get('Status','Unknown'))" 2>/dev/null || echo "Unknown")
        OUT=$(echo "$INVOCATION" | python3 -c "import sys,json; print(json.load(sys.stdin).get('Out',''))" 2>/dev/null || echo "")
        ERR=$(echo "$INVOCATION" | python3 -c "import sys,json; print(json.load(sys.stdin).get('Err',''))" 2>/dev/null || echo "")

        # Print new output since last poll
        if [ ${#OUT} -gt $LAST_OUT_LEN ]; then
            echo "${OUT:$LAST_OUT_LEN}"
        fi
        if [ ${#ERR} -gt $LAST_ERR_LEN ]; then
            echo "${ERR:$LAST_ERR_LEN}" >&2
        fi
        LAST_OUT_LEN=${#OUT}
        LAST_ERR_LEN=${#ERR}

        echo "    [${POLL}/${MAX_POLLS}] Status: $STATUS"

        case "$STATUS" in
            Success|Failed|Cancelled|TimedOut)
                FINAL_STATUS="$STATUS"
                break
                ;;
        esac
    done

    echo ""
    echo "==> Build final status: ${FINAL_STATUS:-Unknown}"

    if [ "$FINAL_STATUS" != "Success" ]; then
        echo "ERROR: Build did not succeed. Final output above." >&2
        exit 1
    fi
fi

# ── 5. Pull + start + health ─────────────────────────────────

echo ""
echo "==> Deploying to EC2 (pull + start + health)..."
"${SCRIPT_DIR}/deploy.sh" "$TAG" "$INSTANCE_ID"

# ── 6. Final status ──────────────────────────────────────────

echo ""
"${SCRIPT_DIR}/status.sh" "$INSTANCE_ID"
