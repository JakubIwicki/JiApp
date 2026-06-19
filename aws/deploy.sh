#!/bin/bash
# JiApp Deploy — uploads config to EC2 and starts the application.
# Run after setup.sh and after building images (build-and-push.sh).
# Usage: ./deploy.sh [--source] [image-tag] [ec2-instance-id]
#   IMAGE_TAG env var or second arg (default: latest — but prefer explicit tag)
set -euo pipefail

REGION="${AWS_REGION:-eu-central-1}"
IMAGE_TAG="${IMAGE_TAG:-${2:-latest}}"
INSTANCE_ID="${3:-${EC2_INSTANCE_ID:-}}"
if [ -z "$INSTANCE_ID" ]; then echo "Usage: $0 [--source] [image-tag] <instance-id>  (or set EC2_INSTANCE_ID)" >&2; exit 1; fi
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
BUCKET="jiapp-deploy-config-${ACCOUNT_ID}"
ECR_BASE="${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/jiapp"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

UPLOAD_SOURCE=false
if [ "${1:-}" = "--source" ]; then
    UPLOAD_SOURCE=true
fi

echo "==> Deploying to EC2: $INSTANCE_ID"

# ── Upload configs to S3 ──────────────────────────────────────

echo ""
echo "==> Uploading configs to S3..."

# EC2 compose (uses images from ECR, not build)
cat > /tmp/jiapp-compose.yml << COMPOSE
services:
  identity:
    image: \${ECR_BASE}/identity:\${IMAGE_TAG}
  ytdownloader:
    image: \${ECR_BASE}/ytdownloader:\${IMAGE_TAG}
  imagetools:
    image: \${ECR_BASE}/imagetools:\${IMAGE_TAG}
  scheduler:
    image: \${ECR_BASE}/scheduler:\${IMAGE_TAG}
  gateway:
    image: \${ECR_BASE}/gateway:\${IMAGE_TAG}
    ports: ["6700:6700"]
    depends_on: [identity, ytdownloader, imagetools, scheduler]
COMPOSE

aws s3 cp /tmp/jiapp-compose.yml "s3://${BUCKET}/ec2/docker-compose.yml" --region "$REGION"
aws s3 cp "${REPO_ROOT}/backend/docker-compose.prod.yml" "s3://${BUCKET}/ec2/docker-compose.prod.yml" --region "$REGION"
aws s3 cp "${REPO_ROOT}/backend/certs/prod/server.pfx" "s3://${BUCKET}/ec2/server.pfx" --region "$REGION"

# Scripts for EC2
for script in startup.sh backup.sh stop-watchdog.sh build-and-push.sh; do
    if [ -f "${SCRIPT_DIR}/${script}" ]; then
        aws s3 cp "${SCRIPT_DIR}/${script}" "s3://${BUCKET}/ec2/${script}" --region "$REGION"
    fi
done

# Systemd units
for unit in jiapp.service jiapp-stop-watchdog.service jiapp-stop-watchdog.timer; do
    if [ -f "${SCRIPT_DIR}/systemd/${unit}" ]; then
        aws s3 cp "${SCRIPT_DIR}/systemd/${unit}" "s3://${BUCKET}/ec2/${unit}" --region "$REGION"
    fi
done

# Source tarball (only with --source flag)
if $UPLOAD_SOURCE; then
    echo "==> Uploading source tarball (this may take a minute)..."
    tar -czf /tmp/jiapp-src.tar.gz \
        --exclude='.artifacts' --exclude='.artifacts-wsl' \
        --exclude='.env' --exclude='certs' --exclude='.data' --exclude='.logs' --exclude='.pids' \
        -C "${REPO_ROOT}" backend/
    aws s3 cp /tmp/jiapp-src.tar.gz "s3://${BUCKET}/ec2/jiapp-src.tar.gz" --region "$REGION"
    rm -f /tmp/jiapp-src.tar.gz
fi

echo "    Configs uploaded to s3://${BUCKET}/ec2/"

# ── Deploy to EC2 via SSM ────────────────────────────────────

echo ""
echo "==> Deploying to EC2..."

DEPLOY_SCRIPT='
set -e
BUCKET=jiapp-deploy-config-'${ACCOUNT_ID}'
REGION='${REGION}'
ACCOUNT='${ACCOUNT_ID}'
ECR_BASE=${ACCOUNT}.dkr.ecr.${REGION}.amazonaws.com/jiapp

mkdir -p /opt/jiapp/{data,logs,certs}
cd /opt/jiapp

echo "Downloading configs..."
aws s3 cp s3://${BUCKET}/ec2/docker-compose.yml . --region ${REGION}
aws s3 cp s3://${BUCKET}/ec2/docker-compose.prod.yml . --region ${REGION}
aws s3 cp s3://${BUCKET}/ec2/server.pfx ./certs/ --region ${REGION}

for f in startup.sh backup.sh stop-watchdog.sh build-and-push.sh; do
    aws s3 cp s3://${BUCKET}/ec2/${f} . --region ${REGION} 2>/dev/null || true
done
chmod +x /opt/jiapp/*.sh

for unit in jiapp.service jiapp-stop-watchdog.service jiapp-stop-watchdog.timer; do
    aws s3 cp s3://${BUCKET}/ec2/${unit} /etc/systemd/system/ --region ${REGION} 2>/dev/null || true
done
systemctl daemon-reload
systemctl enable jiapp.service jiapp-stop-watchdog.timer 2>/dev/null || true
systemctl start jiapp-stop-watchdog.timer 2>/dev/null || true

# Login to ECR and start
aws ecr get-login-password --region ${REGION} | docker login --username AWS --password-stdin ${ACCOUNT}.dkr.ecr.${REGION}.amazonaws.com
set -a; source /opt/jiapp/.env; set +a
export ECR_BASE=${ECR_BASE}
export IMAGE_TAG=${IMAGE_TAG:-latest}

echo "Starting JiApp (IMAGE_TAG=${IMAGE_TAG})..."
docker compose -f docker-compose.yml -f docker-compose.prod.yml pull 2>&1 | tail -5
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d 2>&1

echo "Waiting for services..."
for i in $(seq 1 15); do
    if curl -sk https://localhost:6700/health 2>/dev/null | grep -qi healthy; then
        echo "Gateway healthy after ${i}0s!"
        break
    fi
    sleep 2
done

docker compose -f docker-compose.yml -f docker-compose.prod.yml ps
echo "Deploy complete"
'

aws ssm send-command --region "$REGION" --instance-ids "$INSTANCE_ID" \
    --document-name "AWS-RunShellScript" \
    --comment "JiApp deploy" \
    --parameters "commands=[\"${DEPLOY_SCRIPT}\"]" \
    --query "Command.CommandId" --output text > /dev/null

echo ""
echo "    Deploy command sent. Check status with:"
echo "    ./aws/status.sh $INSTANCE_ID"
