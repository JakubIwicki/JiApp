#!/bin/bash
# JiApp startup — pulls deploy tag from S3, starts Docker Compose.
# Runs on every EC2 boot via systemd (jiapp.service).
set -euo pipefail

# Signal the health watchdog to stand down while we (re)start the stack.
touch /tmp/jiapp_deploying
trap 'rm -f /tmp/jiapp_deploying' EXIT

ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
REGION=$(curl -s http://169.254.169.254/latest/meta-data/placement/region)
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

# Login to ECR
aws ecr get-login-password --region "${REGION}" \
  | docker login --username AWS --password-stdin "${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com"

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
