#!/bin/bash
# Phase 1: AWS Infrastructure Setup
# Run this AFTER: aws configure (with eu-central-1 region)
set -euo pipefail

REGION="${AWS_REGION:-eu-central-1}"
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
echo "==> AWS Account: $ACCOUNT_ID | Region: $REGION"

# ── 1. Deploy CloudFormation stack (S3, ECR, IAM, security group) ──

echo ""
echo "==> [1/4] Deploying CloudFormation stack (ECR, S3, IAM, SG)..."
aws cloudformation deploy \
    --region "$REGION" \
    --stack-name jiapp-infra \
    --template-file "$(dirname "$0")/cloudformation.yml" \
    --capabilities CAPABILITY_IAM \
    --no-fail-on-empty-changeset

echo "    Stack deployed. Fetching outputs..."
SG_ID=$(aws cloudformation describe-stacks \
    --region "$REGION" \
    --stack-name jiapp-infra \
    --query "Stacks[0].Outputs[?OutputKey=='SecurityGroupId'].OutputValue" \
    --output text)
INSTANCE_PROFILE_ARN=$(aws cloudformation describe-stacks \
    --region "$REGION" \
    --stack-name jiapp-infra \
    --query "Stacks[0].Outputs[?OutputKey=='Ec2InstanceProfileArn'].OutputValue" \
    --output text)
LAMBDA_ROLE_ARN=$(aws cloudformation describe-stacks \
    --region "$REGION" \
    --stack-name jiapp-infra \
    --query "Stacks[0].Outputs[?OutputKey=='LambdaStarterRoleArn'].OutputValue" \
    --output text)
GITHUB_ROLE_ARN=$(aws cloudformation describe-stacks \
    --region "$REGION" \
    --stack-name jiapp-infra \
    --query "Stacks[0].Outputs[?OutputKey=='GitHubDeployRoleArn'].OutputValue" \
    --output text)

echo "    SecurityGroup:  $SG_ID"
echo "    EC2 Profile:    $INSTANCE_PROFILE_ARN"
echo "    Lambda Role:    $LAMBDA_ROLE_ARN"
echo "    GitHub Role:    $GITHUB_ROLE_ARN"

# ── 2. Launch EC2 instance ─────────────────────────────────

echo ""
echo "==> [2/4] Launching EC2 t4g.nano..."

# Get latest Amazon Linux 2023 ARM AMI
AMI_ID=$(aws ec2 describe-images \
    --region "$REGION" \
    --owners amazon \
    --filters \
        "Name=name,Values=al2023-ami-minimal-2023*-kernel-6.1-arm64" \
        "Name=state,Values=available" \
        "Name=architecture,Values=arm64" \
    --query "Images | sort_by(@, &CreationDate) | [-1].ImageId" \
    --output text)
echo "    AMI: $AMI_ID"

INSTANCE_ID=$(aws ec2 run-instances \
    --region "$REGION" \
    --image-id "$AMI_ID" \
    --instance-type t4g.nano \
    --security-group-ids "$SG_ID" \
    --iam-instance-profile Arn="$INSTANCE_PROFILE_ARN" \
    --block-device-mappings '[{"DeviceName":"/dev/xvda","Ebs":{"VolumeSize":30,"VolumeType":"gp3","Encrypted":true}}]' \
    --user-data "#!/bin/bash
# Bootstrap placeholder — full setup runs in Phase 3
dnf install -y docker
systemctl enable docker
usermod -aG docker ec2-user
mkdir -p /opt/jiapp/{data,logs,certs}
echo 'Instance bootstrapped — awaiting Phase 3 deploy'" \
    --tag-specifications 'ResourceType=instance,Tags=[{Key=Name,Value=JiApp}]' \
    --query "Instances[0].InstanceId" \
    --output text)

echo "    Instance: $INSTANCE_ID"

# Wait for running
echo "    Waiting for instance to be running..."
aws ec2 wait instance-running --region "$REGION" --instance-ids "$INSTANCE_ID"

# ── 3. Allocate + associate Elastic IP ─────────────────────

echo ""
echo "==> [3/4] Allocating Elastic IP..."
ALLOC_ID=$(aws ec2 allocate-address \
    --region "$REGION" \
    --domain vpc \
    --query "AllocationId" \
    --output text)
EIP=$(aws ec2 describe-addresses \
    --region "$REGION" \
    --allocation-ids "$ALLOC_ID" \
    --query "Addresses[0].PublicIp" \
    --output text)

aws ec2 associate-address \
    --region "$REGION" \
    --instance-id "$INSTANCE_ID" \
    --allocation-id "$ALLOC_ID"

echo "    Elastic IP: $EIP → $INSTANCE_ID"

# ── 4. Deploy Lambda + API Gateway ─────────────────────────

echo ""
echo "==> [4/4] Deploying Lambda starter + API Gateway..."

# Package Lambda
LAMBDA_DIR="$(dirname "$0")/lambda"
ZIP_FILE="/tmp/jiapp-starter.zip"
(cd "$LAMBDA_DIR" && zip -j "$ZIP_FILE" starter.py)

# Create Lambda
aws lambda create-function \
    --region "$REGION" \
    --function-name jiapp-starter \
    --runtime python3.12 \
    --role "$LAMBDA_ROLE_ARN" \
    --handler starter.handler \
    --zip-file "fileb://$ZIP_FILE" \
    --environment "Variables={EC2_INSTANCE_ID=$INSTANCE_ID}" \
    --timeout 10 \
    --no-cli-pager 2>&1 || {
    # Function may already exist — update it
    echo "    Lambda exists, updating..."
    aws lambda update-function-code \
        --region "$REGION" \
        --function-name jiapp-starter \
        --zip-file "fileb://$ZIP_FILE"
    aws lambda update-function-configuration \
        --region "$REGION" \
        --function-name jiapp-starter \
        --environment "Variables={EC2_INSTANCE_ID=$INSTANCE_ID}"
}

# Add API Gateway permission
aws lambda add-permission \
    --region "$REGION" \
    --function-name jiapp-starter \
    --statement-id api-gateway-invoke \
    --action lambda:InvokeFunction \
    --principal apigateway.amazonaws.com \
    --source-arn "arn:aws:execute-api:$REGION:$ACCOUNT_ID:*/*/start" \
    2>/dev/null || echo "    Permission already exists"

# Create HTTP API
API_ID=$(aws apigatewayv2 create-api \
    --region "$REGION" \
    --name jiapp-wake \
    --protocol-type HTTP \
    --target "arn:aws:lambda:$REGION:$ACCOUNT_ID:function:jiapp-starter" \
    --query "ApiId" \
    --output text)

# Create route
aws apigatewayv2 create-route \
    --region "$REGION" \
    --api-id "$API_ID" \
    --route-key "POST /start" \
    --target "integrations/$(
        aws apigatewayv2 create-integration \
            --region "$REGION" \
            --api-id "$API_ID" \
            --integration-type AWS_PROXY \
            --integration-uri "arn:aws:lambda:$REGION:$ACCOUNT_ID:function:jiapp-starter" \
            --payload-format-version "2.0" \
            --query "IntegrationId" \
            --output text
    )" > /dev/null

# Create stage
aws apigatewayv2 create-stage \
    --region "$REGION" \
    --api-id "$API_ID" \
    --stage-name '$default' \
    --auto-deploy > /dev/null

# Configure CORS
aws apigatewayv2 update-api \
    --region "$REGION" \
    --api-id "$API_ID" \
    --cors-configuration '{"AllowOrigins":["*"],"AllowMethods":["POST","OPTIONS"],"AllowHeaders":["*"]}' > /dev/null

API_URL="https://${API_ID}.execute-api.${REGION}.amazonaws.com"
echo "    API Gateway: $API_URL/start"

# ── Done ──

rm -f "$ZIP_FILE"

echo ""
echo "════════════════════════════════════════════════════════════"
echo " Phase 1 Complete"
echo "════════════════════════════════════════════════════════════"
echo ""
echo "  EC2 Instance:    $INSTANCE_ID"
echo "  Elastic IP:      $EIP"
echo "  Security Group:  $SG_ID"
echo "  Wake-up URL:     $API_URL/start"
echo "  ECR Registry:    ${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/jiapp/*"
echo "  S3 Backups:      s3://jiapp-backups-${ACCOUNT_ID}/"
echo "  S3 Deploy Config: s3://jiapp-deploy-config-${ACCOUNT_ID}/"
echo ""
echo "  GitHub Actions Role ARN (for deploy.yml):"
echo "    $GITHUB_ROLE_ARN"
echo ""
echo "  Test wake-up:"
echo "    curl -X POST $API_URL/start"
echo ""
echo "  View instance state:"
echo "    aws ec2 describe-instances --instance-ids $INSTANCE_ID --query 'Reservations[0].Instances[0].State.Name'"
echo ""
echo "  IMPORTANT: Save EC2_INSTANCE_ID and EIP for Phases 2-4."
echo "    EC2_INSTANCE_ID=$INSTANCE_ID"
echo "    ELASTIC_IP=$EIP"
