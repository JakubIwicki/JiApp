#!/bin/bash
# JiApp startup — pulls deploy tag from S3, starts Docker Compose.
# Runs on every EC2 boot via systemd (jiapp.service).
set -euo pipefail

# Signal the health watchdog to stand down while we (re)start the stack.
touch /tmp/jiapp_deploying
trap 'rm -f /tmp/jiapp_deploying' EXIT

# Retry AWS credential check — IMDS can lag behind boot on t4g/ARM
ACCOUNT_ID=""
for i in $(seq 1 12); do
    if ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text 2>/dev/null); then
        break
    fi
    echo "[$(date)] Waiting for AWS credentials... attempt $i/12"
    sleep 5
done
if [ -z "$ACCOUNT_ID" ]; then
    echo "[$(date)] ERROR: AWS credentials unavailable after 12 attempts" >&2
    exit 1
fi
TOKEN=$(curl -s -X PUT "http://169.254.169.254/latest/api/token" -H "X-aws-ec2-metadata-token-ttl-seconds: 60" 2>/dev/null || echo "")
REGION=$(curl -s -H "X-aws-ec2-metadata-token: ${TOKEN}" http://169.254.169.254/latest/meta-data/placement/region 2>/dev/null || echo "eu-central-1")
BUCKET="jiapp-deploy-config-${ACCOUNT_ID}"

echo "[$(date)] JiApp startup — phase 0: fetch deploy config"

# Ensure /opt/jiapp/data and logs exist
mkdir -p /opt/jiapp/{data,logs}
cd /opt/jiapp

# Source secrets first (JWT_KEY, CERT_PASSWORD, etc.) — may contain stale IMAGE_TAG
set -a; source /opt/jiapp/.env; set +a

# Pull latest deploy tag from S3 (takes priority over any stale IMAGE_TAG in .env)
IMAGE_TAG=$(aws s3 cp "s3://${BUCKET}/current-tag.txt" - 2>/dev/null || echo "latest")
echo "[$(date)] Deploying IMAGE_TAG=${IMAGE_TAG}"

# Login to ECR (retry — credentials can expire mid-script or transient network issues)
for i in $(seq 1 6); do
    if aws ecr get-login-password --region "${REGION}" 2>/dev/null \
        | docker login --username AWS --password-stdin "${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com" 2>/dev/null; then
        break
    fi
    echo "[$(date)] ECR login failed — retry $i/6"
    sleep 5
done

# Export for docker compose interpolation
export IMAGE_TAG
export ECR_BASE="${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/jiapp"

# Pull images
echo "[$(date)] Pulling images..."
docker compose -f docker-compose.yml -f docker-compose.prod.yml pull 2>&1 | tail -5

# Start services
echo "[$(date)] Starting services..."
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Wait for Gateway health check (max 60s)
echo "[$(date)] Waiting for Gateway health check..."
for i in $(seq 1 30); do
    if curl -sk https://localhost:6700/health 2>/dev/null | grep -qi 'healthy'; then
        echo "[$(date)] Gateway healthy after ${i}s"
        break
    fi
    sleep 2
done

echo "[$(date)] Startup complete — IMAGE_TAG=${IMAGE_TAG}"
