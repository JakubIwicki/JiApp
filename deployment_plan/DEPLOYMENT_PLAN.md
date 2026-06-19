# JiApp AWS Deployment Plan

## Context

JiApp backend is a .NET 10 microservices app (5 services: Gateway, Identity, YtDownloader, ImageTools, Scheduler) behind a YARP reverse proxy. Currently Docker Compose-based with dual database provider (PostgreSQL/SQLite). CI exists (build + test on PRs) but stops at `docker build` — no CD pipeline, no cloud IaC, no automated deployment. The app will see minimal usage (~1 hour/week, max 3 concurrent users) for the foreseeable future.

**Goal:** Automatic deployment from this repo to AWS at the lowest possible cost, with the EC2 instance staying stopped when idle and waking up on-demand when a mobile user opens the app.

---

## Architecture Overview

```
Mobile App                    AWS Cloud
┌──────────────┐           ┌──────────────────────────────────────────┐
│              │           │                                          │
│  POST /start │──────────▶│  API Gateway HTTP API → Lambda "starter" │
│  (wake-up)   │           │       │                                  │
│              │           │       │ ec2:StartInstances               │
│  "Server     │           │       ▼                                  │
│  warming up" │           │  ┌──────────────────────────────────┐   │
│              │           │  │   EC2 t4g.nano (ARM, 0.5GB RAM)  │   │
│  poll GET /  │◀──────────│  │   ┌────────────────────────┐     │   │
│  health      │           │  │   │ Docker Compose (5 svc) │     │   │
│              │           │  │   │ Gateway     :6700      │     │   │
│  GET/POST    │──────────▶│  │   │ Identity    :6701      │     │   │
│  /api/v1/*   │           │  │   │ YtDownloader:6702      │     │   │
│              │           │  │   │ ImageTools  :6703      │     │   │
│              │           │  │   │ Scheduler   :6704      │     │   │
│              │           │  │   │ All use SQLite (.db)   │     │   │
│              │           │  │   └────────────────────────┘     │   │
│              │           │  │                                   │   │
│              │           │  │  Auto-stop watchdog (20 min idle) │   │
│              │           │  └──────────────────────────────────┘   │
│              │           │                                          │
│              │           │  ┌──────────┐   ┌────────────────────┐  │
│              │           │  │   ECR    │   │        S3          │  │
│              │           │  │ (images) │   │  DB backups        │  │
│              │           │  │          │   │  deploy config     │  │
│              │           │  └──────────┘   └────────────────────┘  │
│              │           └──────────────────────────────────────────┘
└──────────────┘
```

### Key Design Decisions

- **SQLite only** — PostgreSQL dropped entirely. Removes RAM pressure, eliminates WAL-corruption-on-stop risk, makes t4g.nano viable. App already runs SQLite in dev with EF Core migrations + WAL mode.
- **EC2 stopped ~95% of the time** — started on-demand by mobile, auto-stops after 20 min of genuine idle.
- **Git SHA image tagging** — never `:latest`. ECR lifecycle keeps last 5. Rollback = change tag.
- **Baked CA cert for TLS** — generate production CA, sign server cert, bake CA into APK. Works on raw IP, no DNS needed, zero recurring cost.

---

## AWS Services & Cost

| Service | Purpose | Monthly Cost |
|---------|---------|-------------|
| EC2 t4g.nano | Docker host, ~5 hrs/month | $0.03 |
| EBS 30GB gp3 | OS + images + DBs + logs (KMS encrypted) | $2.40 |
| Elastic IP | Static public IP | $0.00 (attached to stopped instance) |
| ECR | Docker registry, lifecycle keeps last 5 | $0.10 |
| S3 | DB backups (30-day), deploy config | $0.05 |
| API Gateway HTTP API | Wake-up endpoint | $0.00 (free tier) |
| Lambda | Starter function | $0.00 (free tier) |
| **TOTAL** | | **~$2.58/month** |

**vs always-on:** $2.43 vs $6.14 — saves 60%.

---

## TLS — Baked CA Certificate (Primary)

Generate production CA → sign server cert → bake CA into APK → Kestrel serves HTTPS.

```
openssl req -x509 -newkey rsa:4096 -days 1095 -nodes \
  -keyout jiapp-ca.key -out jiapp-ca.crt \
  -subj "/CN=JiApp Production CA"

openssl req -new -newkey rsa:2048 -nodes \
  -keyout jiapp-server.key -out jiapp-server.csr \
  -subj "/CN=JiApp Server"

openssl x509 -req -in jiapp-server.csr \
  -CA jiapp-ca.crt -CAkey jiapp-ca.key -CAcreateserial \
  -out jiapp-server.crt -days 1095

openssl pkcs12 -export -out server.pfx \
  -inkey jiapp-server.key -in jiapp-server.crt \
  -certfile jiapp-ca.crt -passout pass:${CERT_PASSWORD}
```

CA cert baked into APK (`res/raw/jiapp_prod_ca`), server PFX mounted into Gateway container. Renewal every 2-3 years via APK update.

**Side option — Cloudflare Tunnel (future):** See [CLOUDFLARE_TUNNEL.md](CLOUDFLARE_TUNNEL.md).

---

## EC2 Instance Configuration

- **Type:** t4g.nano (ARM64, 0.5GB RAM)
- **AMI:** Amazon Linux 2023 ARM
- **EBS:** 30GB gp3, KMS encrypted
- **Region:** eu-central-1 (Frankfurt)
- **Management:** SSM Session Manager (no SSH)

### Software Stack

```
/opt/jiapp/
  ├── docker-compose.yml          (base)
  ├── docker-compose.prod.yml     (SQLite, HTTPS)
  ├── .env                        (secrets)
  ├── certs/server.pfx
  ├── data/*.db                   (SQLite, persisted)
  ├── logs/
  ├── startup.sh                  (pull tag from S3, compose up)
  ├── backup.sh                   (backup .db to S3)
  └── stop-watchdog.sh            (idle detect → backup → stop)
```

---

## SQLite Migration

Remove PostgreSQL entirely. The app already supports this — dev runs SQLite, dual-provider pattern detects connection string format.

**Changes:**
1. `docker-compose.prod.yml`: Remove `postgres`, switch all connection strings to SQLite
2. `docker-compose.yml`: Remove postgres (or behind `production: false` profile)
3. Delete `docker/init-databases.sh`
4. Verify EF migrations run on cold start (Identity/Scheduler already call `db.Database.Migrate()`)

---

## Auto-Start — Lambda + API Gateway

```
Mobile → POST /start → API Gateway → Lambda → ec2.start_instances()
      ← { state: "pending", estimatedWait: 60 }
```

Lambda (Python, ~40 lines) checks instance state (idempotent), starts if stopped. API Gateway HTTP API, no auth, CORS enabled.

---

## Auto-Stop Watchdog

**Idle = all true for 20 consecutive minutes (checked every 60s):**
1. No yt-dlp or ffmpeg processes
2. Zero TCP connections to :6700

**Graceful shutdown sequence:**
1. `backup.sh` → upload .db files to S3
2. `docker compose stop --timeout 30`
3. `aws ec2 stop-instances`

---

## Database Backup

`backup.sh` compresses and uploads each `.db` file to `s3://jiapp-backups-{account}/db-backups/{name}/{timestamp}.db.gz`. 30-day S3 lifecycle.

---

## Docker Image Strategy

- **Tag:** git short SHA (`abc123f`), never `:latest`
- **ECR lifecycle:** keep last 5 images
- **Rollback:** change `IMAGE_TAG` in `/opt/jiapp/.env`
- **Deploy to stopped instance:** write tag to S3 `current-tag.txt`, startup.sh reads it on next boot

---

## Mobile Cold Start UX

```
User opens app → "Waking server..." spinner → POST /start (Lambda)
  → poll GET /health every 3s (10s timeout each)
  → 200 OK → proceed to login
  → 120s total timeout → "Server unavailable" + retry button
```

**Timing:** Warm start 45-90s, cold (first deploy) 3-8 min.

---

## CI/CD — `deploy.yml`

Triggered on push to `main` + manual `workflow_dispatch`:

1. **build-and-push:** Build Docker images via `docker compose`, tag with git SHA, push to ECR
2. **deploy:** If EC2 running → SSM Run Command to pull + restart. If stopped → write tag to S3.

Uses GitHub OIDC → AWS IAM (no long-lived access keys).

---

## Secrets Management

| Secret | Location | How |
|--------|----------|-----|
| JWT_KEY | /opt/jiapp/.env | Manual once via SSM |
| YOUTUBE_API_KEY | /opt/jiapp/.env | Manual once via SSM |
| IMAGE_TAG | /opt/jiapp/.env + S3 | CI writes on deploy |
| Cert password | /opt/jiapp/.env | Manual once |
| AWS creds (EC2) | Instance profile | Auto via IAM |
| AWS creds (CI) | GitHub OIDC → IAM | No keys stored |

EBS volume is KMS-encrypted at rest.

---

## Logging

Serilog writes structured JSON to files on EBS (`/app/logs/jiapp-{service}-.log`), 7-day retention. Docker logs via journald. System scripts log to `/opt/jiapp/logs/system.log`.

---

## IAM Roles

**EC2 (`jiapp-ec2-role`):** ECR pull, S3 get/put (backups + deploy config), `ec2:StopInstances`. Explicit `Deny ec2:TerminateInstances` (prevents EIP cost leak).

**Lambda (`jiapp-starter-role`):** `ec2:StartInstances`, `ec2:DescribeInstances`, CloudWatch Logs.

**GitHub OIDC (`github-actions-deploy`):** ECR push, SSM SendCommand, S3 put (current-tag.txt), `ec2:DescribeInstances`.

---

## Security Group

| Direction | Port | Purpose |
|-----------|------|---------|
| Inbound | TCP 6700 | Gateway HTTPS |
| Outbound | TCP 443 | ECR, S3, YouTube API |
| Outbound | TCP 80 | yt-dlp downloads |

No SSH inbound — all management via SSM.

---

## Implementation Phases

### Phase 1: AWS Infrastructure Setup
1. Create S3 buckets + ECR repositories (5 services)
2. Set ECR lifecycle policies (keep last 5)
3. Create IAM roles (EC2, Lambda, GitHub OIDC)
4. Configure GitHub OIDC provider
5. Set up API Gateway HTTP API + Lambda starter
6. Launch EC2 t4g.nano, attach Elastic IP
7. Configure security group

### Phase 2: Application Changes
1. Generate production CA + server PFX
2. Update Gateway `appsettings.Production.json` for HTTPS
3. Revise `docker-compose.prod.yml`: SQLite, remove PostgreSQL
4. Remove `docker/init-databases.sh`
5. Update service connection strings
6. Bake CA cert into mobile APK
7. Add mobile `ServerWakeScreen` + polling logic

### Phase 3: EC2 Bootstrap
1. Write `startup.sh`, `backup.sh`, `stop-watchdog.sh`
2. Write systemd units (jiapp.service, backup.timer, watchdog.timer)
3. Write cloud-init user data
4. Test: launch → start → health → watchdog → stop

### Phase 4: CI/CD Pipeline
1. Create `.github/workflows/deploy.yml`
2. Test: push → ECR → S3 tag → EC2 start → health → auto-stop
3. Test rollback procedure

### Phase 5: Polish
1. Update URLS.md with production endpoints
2. Update README.md
3. Document rollback + disaster recovery

---

## Risk Registry

| # | Severity | Risk | Mitigation |
|---|----------|------|------------|
| 1 | High | t4g.nano OOM with 5 .NET services | DOTNET_GCHeapHardLimit; SQLite instead of PostgreSQL; fallback: t4g.micro |
| 2 | High | AWS capacity unavailable on start | Lambda retries in different AZ; fallback: t4g.micro |
| 3 | Medium | Bad deploy breaks app | Git SHA tags + ECR keeps last 5; rollback via S3 tag change |
| 4 | Medium | EBS fills (MP3s + DB growth) | YtDownloader cleanup; watchdog disk check at 80% |
| 5 | Medium | Instance terminated instead of stopped | IAM Deny ec2:TerminateInstances |
| 6 | Medium | Auto-stop kills active download | Watchdog checks yt-dlp/ffmpeg process tree + connections |
| 7 | Low | EIP cost if terminated | IAM Deny + CloudWatch alarm |
| 8 | Low | API key leak | EBS encrypted; .env gitignored |
| 9 | Low | Cert expiry (3 years) | Documented renewal procedure; APK update |

---

## Verification Checklist

- [ ] Phase 1: EC2 launches via Lambda, EIP responds, IAM roles functional
- [ ] Phase 2: Gateway serves HTTPS (trusted on mobile), SQLite migrations run on start
- [ ] Phase 3: EC2 boots from scratch (cloud-init), services start, watchdog stops after 20 min
- [ ] Phase 4: Push → images in ECR → S3 tag → EC2 pulls correct tag → health → auto-stop
- [ ] Phase 5: E2E: user opens mobile → "waking server" → login works → idle 20 min → server stops
