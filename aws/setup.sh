#!/bin/bash
# JiApp AWS Infrastructure Setup
# Run once: creates all AWS resources (ECR, S3, IAM, EC2, Lambda, API Gateway)
# Prerequisites: aws configure (eu-central-1)
set -euo pipefail

REGION="${AWS_REGION:-eu-central-1}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
echo "==> AWS Account: $ACCOUNT_ID | Region: $REGION"

# ── 1. CloudFormation stack (ECR, S3, IAM, SG) ─────────────────

echo ""
echo "==> [1/4] Deploying CloudFormation stack..."
aws cloudformation deploy \
    --region "$REGION" \
    --stack-name jiapp-infra \
    --template-file "${SCRIPT_DIR}/cloudformation.yml" \
    --capabilities CAPABILITY_IAM CAPABILITY_NAMED_IAM \
    --no-fail-on-empty-changeset

# Fetch outputs
SG_ID=$(aws cloudformation describe-stacks --region "$REGION" --stack-name jiapp-infra \
    --query "Stacks[0].Outputs[?OutputKey=='SecurityGroupId'].OutputValue" --output text)
INSTANCE_PROFILE=$(aws cloudformation describe-stacks --region "$REGION" --stack-name jiapp-infra \
    --query "Stacks[0].Outputs[?OutputKey=='Ec2InstanceProfileArn'].OutputValue" --output text)
LAMBDA_ROLE_ARN=$(aws cloudformation describe-stacks --region "$REGION" --stack-name jiapp-infra \
    --query "Stacks[0].Outputs[?OutputKey=='LambdaStarterRoleArn'].OutputValue" --output text)
GITHUB_ROLE_ARN=$(aws cloudformation describe-stacks --region "$REGION" --stack-name jiapp-infra \
    --query "Stacks[0].Outputs[?OutputKey=='GitHubDeployRoleArn'].OutputValue" --output text)

echo "    SG: $SG_ID"
echo "    Lambda Role: $LAMBDA_ROLE_ARN"
echo "    GitHub Role: $GITHUB_ROLE_ARN"

# ── S3: Downloads bucket (public-read for APK) ──────────────

DOWNLOADS_BUCKET="jiapp-downloads-${ACCOUNT_ID}"
echo ""
echo "==> Configuring downloads bucket: $DOWNLOADS_BUCKET"

# Ensure bucket exists (created by CloudFormation; idempotent create if not)
aws s3api head-bucket --bucket "$DOWNLOADS_BUCKET" --region "$REGION" 2>/dev/null || \
    aws s3api create-bucket --bucket "$DOWNLOADS_BUCKET" --region "$REGION" \
        --create-bucket-configuration "LocationConstraint=$REGION"

# Public-access-block: block ACLs but ALLOW public bucket-policy reads
aws s3api put-public-access-block --bucket "$DOWNLOADS_BUCKET" \
    --public-access-block-configuration \
        "BlockPublicAcls=true,BlockPublicPolicy=false,IgnorePublicAcls=true,RestrictPublicBuckets=false"

# Bucket policy: public-read s3:GetObject on bucket objects only
aws s3api put-bucket-policy --bucket "$DOWNLOADS_BUCKET" \
    --policy '{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Principal": "*",
    "Action": "s3:GetObject",
    "Resource": "arn:aws:s3:::'"$DOWNLOADS_BUCKET"'/*"
  }]
}'

# CORS: allow GET of apk-metadata.json (and APK) from Pages + localhost dev
aws s3api put-bucket-cors --bucket "$DOWNLOADS_BUCKET" \
    --cors-configuration '{
  "CORSRules": [{
    "AllowedOrigins": ["https://jakubiwicki.github.io", "http://localhost:5173"],
    "AllowedMethods": ["GET"],
    "AllowedHeaders": ["*"],
    "MaxAgeSeconds": 3600
  }]
}'

echo "    Downloads bucket ready: s3://${DOWNLOADS_BUCKET}/"

# ── 2. EC2 instance ───────────────────────────────────────────

echo ""
echo "==> [2/4] Launching EC2 (t4g.small, ARM)..."
AMI_ID=$(aws ec2 describe-images --region "$REGION" --owners amazon \
    --filters "Name=name,Values=al2023-ami-minimal-2023*-kernel-6.1-arm64" \
              "Name=state,Values=available" "Name=architecture,Values=arm64" \
    --query "Images | sort_by(@, &CreationDate) | [-1].ImageId" --output text)

USER_DATA=$(base64 -w0 << 'BOOTSTRAP'
#!/bin/bash
dnf install -y docker amazon-ssm-agent
systemctl enable docker amazon-ssm-agent
systemctl start docker amazon-ssm-agent
usermod -aG docker ec2-user
mkdir -p /opt/jiapp/{data,logs,certs}
BOOTSTRAP
)

INSTANCE_ID=$(aws ec2 run-instances \
    --region "$REGION" --image-id "$AMI_ID" --instance-type t4g.small \
    --security-group-ids "$SG_ID" --iam-instance-profile Arn="$INSTANCE_PROFILE" \
    --block-device-mappings '[{"DeviceName":"/dev/xvda","Ebs":{"VolumeSize":30,"VolumeType":"gp3","Encrypted":true}}]' \
    --user-data "$USER_DATA" \
    --tag-specifications 'ResourceType=instance,Tags=[{Key=Name,Value=JiApp}]' \
    --query "Instances[0].InstanceId" --output text)

echo "    Instance: $INSTANCE_ID"
aws ec2 wait instance-running --region "$REGION" --instance-ids "$INSTANCE_ID"

# ── 3. Elastic IP ─────────────────────────────────────────────

echo ""
echo "==> [3/4] Allocating Elastic IP..."
ALLOC_ID=$(aws ec2 allocate-address --region "$REGION" --domain vpc --query "AllocationId" --output text)
EIP=$(aws ec2 describe-addresses --region "$REGION" --allocation-ids "$ALLOC_ID" --query "Addresses[0].PublicIp" --output text)
aws ec2 associate-address --region "$REGION" --instance-id "$INSTANCE_ID" --allocation-id "$ALLOC_ID"
echo "    $EIP -> $INSTANCE_ID"

# ── 4. Lambda + API Gateway ───────────────────────────────────

echo ""
echo "==> [4/4] Deploying Lambda + API Gateway..."
LAMBDA_DIR="${SCRIPT_DIR}/lambda"
ZIP_FILE="/tmp/jiapp-starter.zip"
(cd "$LAMBDA_DIR" && zip -j "$ZIP_FILE" starter.py)

aws lambda create-function --region "$REGION" --function-name jiapp-starter \
    --runtime python3.12 --role "$LAMBDA_ROLE_ARN" --handler starter.handler \
    --zip-file "fileb://$ZIP_FILE" --timeout 10 \
    --environment "Variables={EC2_INSTANCE_ID=$INSTANCE_ID}" --no-cli-pager 2>&1 || {
    aws lambda update-function-code --region "$REGION" --function-name jiapp-starter --zip-file "fileb://$ZIP_FILE"
    aws lambda update-function-configuration --region "$REGION" --function-name jiapp-starter \
        --environment "Variables={EC2_INSTANCE_ID=$INSTANCE_ID}"
}

aws lambda add-permission --region "$REGION" --function-name jiapp-starter \
    --statement-id api-gateway-invoke --action lambda:InvokeFunction \
    --principal apigateway.amazonaws.com \
    --source-arn "arn:aws:execute-api:$REGION:$ACCOUNT_ID:*/*/start" 2>/dev/null || true

API_ID=$(aws apigatewayv2 create-api --region "$REGION" --name jiapp-wake \
    --protocol-type HTTP --query "ApiId" --output text)
INTEGRATION_ID=$(aws apigatewayv2 create-integration --region "$REGION" --api-id "$API_ID" \
    --integration-type AWS_PROXY --payload-format-version "2.0" \
    --integration-uri "arn:aws:lambda:$REGION:$ACCOUNT_ID:function:jiapp-starter" \
    --query "IntegrationId" --output text)
aws apigatewayv2 create-route --region "$REGION" --api-id "$API_ID" \
    --route-key "POST /start" --target "integrations/$INTEGRATION_ID" > /dev/null
aws apigatewayv2 create-stage --region "$REGION" --api-id "$API_ID" \
    --stage-name '$default' --auto-deploy > /dev/null 2>&1 || true
aws apigatewayv2 update-api --region "$REGION" --api-id "$API_ID" \
    --cors-configuration '{"AllowOrigins":["*"],"AllowMethods":["POST","OPTIONS"],"AllowHeaders":["*"]}' > /dev/null

rm -f "$ZIP_FILE"
API_URL="https://${API_ID}.execute-api.${REGION}.amazonaws.com"

# ── Done ──

echo ""
echo "════════════════════════════════════════════════════════"
echo " Setup Complete"
echo "════════════════════════════════════════════════════════"
echo ""
echo "  EC2 Instance:    $INSTANCE_ID"
echo "  Elastic IP:      $EIP"
echo "  Wake-up URL:     $API_URL/start"
echo "  GitHub Role ARN: $GITHUB_ROLE_ARN"
echo ""
echo "  Save these for deploy.sh:"
echo "    EC2_INSTANCE_ID=$INSTANCE_ID"
echo "    ELASTIC_IP=$EIP"
