# JiApp AWS Tools

Operational scripts for managing the JiApp AWS deployment.

## Setup

```bash
# One-time: create all AWS infrastructure
./aws/setup.sh
```

## Daily Operations

```bash
# Check deployment status (EC2 state, containers, health, logs)
./aws/status.sh

# View logs
./aws/logs.sh app          # Gateway application logs
./aws/logs.sh build        # Docker build logs
./aws/logs.sh watchdog     # Auto-stop watchdog
./aws/logs.sh compose      # Docker Compose logs (all services)

# Deploy new code
./aws/deploy.sh            # Deploy config + pull latest images
./aws/deploy.sh --source   # Also upload source for EC2 build
```

## On the EC2 Instance

These scripts live at `/opt/jiapp/` on the EC2 instance:

| Script | Purpose |
|--------|---------|
| `startup.sh` | Pull images from ECR, start Docker Compose, wait for health |
| `backup.sh` | Compress SQLite databases and upload to S3 |
| `stop-watchdog.sh` | Check idle (yt-dlp + TCP connections), auto-stop after 20 min |
| `build-and-push.sh` | Build ARM64 Docker images from source and push to ECR |

## Architecture

```
Mobile App → API Gateway → Lambda → EC2 (start)
Mobile App → https://<eip>:6700 → Gateway → services
GitHub → deploy.yml → ECR → EC2 (pull)
```

### AWS Resources (filled by setup.sh output)

| Resource | Example |
|----------|---------|
| EC2 | `i-xxxxxxxxxxxxx` (t4g.micro) |
| Elastic IP | `<eip-from-setup-output>` |
| Wake-up URL | `https://<api-id>.execute-api.eu-central-1.amazonaws.com/start` |
| ECR | `<account>.dkr.ecr.eu-central-1.amazonaws.com/jiapp/*` |
| S3 Backups | `jiapp-backups-<account>` |
| S3 Deploy Config | `jiapp-deploy-config-<account>` |

### Certificates

The repository tracks only the CA certificate (`backend/certs/prod/jiapp-ca.crt`)
for mobile app pinning. The server certificate, private key, CSR, and PFX
(`jiapp-server.crt`, `jiapp-server.key`, `jiapp-server.csr`, `server.pfx`)
are generated at deploy time and must never be committed — they embed the
server's Elastic IP in the SAN.

## GitHub CI (not yet enabled)

To enable auto-deploy on push to main:
1. Set GitHub Secrets: `AWS_DEPLOY_ROLE_ARN`, `EC2_INSTANCE_ID`
2. Add `docker buildx` for ARM64 cross-compilation in CI

See `../deployment_plan/DEPLOYMENT_PLAN.md` for full architecture documentation.
