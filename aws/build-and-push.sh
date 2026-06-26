#!/bin/bash
# JiApp build-and-push — builds ARM64 Docker images from source and pushes to ECR.
# Run on the EC2 instance: /opt/jiapp/build-and-push.sh [tag]
set -euo pipefail

# Hold off the health watchdog: a heavy build can starve the gateway and trip a false restart.
touch /tmp/jiapp_deploying

TAG="${1:-$(date -u +%Y%m%d-%H%M%S)}"
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
TOKEN=$(curl -s -X PUT "http://169.254.169.254/latest/api/token" -H "X-aws-ec2-metadata-token-ttl-seconds: 60" 2>/dev/null || echo "")
REGION=$(curl -s -H "X-aws-ec2-metadata-token: ${TOKEN}" http://169.254.169.254/latest/meta-data/placement/region 2>/dev/null || echo "eu-central-1")
ECR_BASE="${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/jiapp"
SOURCE_BUCKET="jiapp-deploy-config-${ACCOUNT_ID}"
WORK_DIR="/opt/jiapp/.build-$$"
trap 'rm -f /tmp/jiapp_deploying; rm -rf "${WORK_DIR}" /opt/jiapp/.jiapp-src.tar.gz' EXIT

echo "[$(date)] Build started — TAG=${TAG}"

# ── 0. Disk preflight + reclaim ──
echo "[$(date)] Disk preflight..."
df -h /opt/jiapp /var/lib/docker 2>/dev/null | sort -u || df -h /
docker image prune -f || true
docker builder prune -f || true
AVAIL_GB=$(df -BG --output=avail /var/lib/docker 2>/dev/null | tail -1 | tr -dc '0-9')
[ -n "${AVAIL_GB}" ] || AVAIL_GB=$(df -BG --output=avail / 2>/dev/null | tail -1 | tr -dc '0-9')
if [ "${AVAIL_GB:-0}" -lt 6 ]; then
    echo "[$(date)] WARNING: ${AVAIL_GB}G free — running aggressive reclaim..."
    docker system prune -af || true
    AVAIL_GB=$(df -BG --output=avail /var/lib/docker 2>/dev/null | tail -1 | tr -dc '0-9')
    [ -n "${AVAIL_GB}" ] || AVAIL_GB=$(df -BG --output=avail / 2>/dev/null | tail -1 | tr -dc '0-9')
    if [ "${AVAIL_GB:-0}" -lt 4 ]; then
        echo "ERROR: <4G free after prune — aborting before build to avoid wedging the box." >&2
        exit 1
    fi
fi
echo "[$(date)] Disk preflight OK — ${AVAIL_GB}G free"

# ── 1. Download & extract source ──
echo "[$(date)] Downloading source..."
mkdir -p "${WORK_DIR}"
aws s3 cp "s3://${SOURCE_BUCKET}/ec2/jiapp-src.tar.gz" /opt/jiapp/.jiapp-src.tar.gz --region "${REGION}"
tar -xzf /opt/jiapp/.jiapp-src.tar.gz -C "${WORK_DIR}"
echo "[$(date)] Source extracted"

# ── 2. Build images ──
echo "[$(date)] Building Docker images..."
cd "${WORK_DIR}/backend"
declare -A SVC_DIR=(
    [identity]=Identity
    [ytdownloader]=YtDownloader
    [imagetools]=ImageTools
    [scheduler]=Scheduler
    [gateway]=Gateway
)

for svc in identity ytdownloader imagetools scheduler gateway; do
    dir="${SVC_DIR[$svc]}"
    echo "  → Building jiapp/${svc}..."
    docker build \
        -t "${ECR_BASE}/${svc}:${TAG}" \
        -t "${ECR_BASE}/${svc}:latest" \
        -f "./src/JiApp.${dir}/Dockerfile" \
        .
done
echo "[$(date)] Build complete"

# ── 3. Login + push to ECR ──
echo "[$(date)] Pushing to ECR..."
aws ecr get-login-password --region "${REGION}" \
    | docker login --username AWS --password-stdin "${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com"

for svc in identity ytdownloader imagetools scheduler gateway; do
    echo "  → Pushing ${svc}:${TAG}..."
    docker push "${ECR_BASE}/${svc}:${TAG}"
done
echo "[$(date)] Push complete"

# ── 4. Update deploy tag ──
echo "${TAG}" | aws s3 cp - "s3://${SOURCE_BUCKET}/current-tag.txt" --region "${REGION}"
echo "${TAG}" > /opt/jiapp/.tag

echo "[$(date)] Done — TAG=${TAG}"
