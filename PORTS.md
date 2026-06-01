# JiApp — Port Assignments

## Architecture

All traffic enters through the **API Gateway** (YARP reverse proxy). The Gateway authenticates requests, enforces rate limits, adds correlation IDs, and forwards to downstream microservices.

```
Mobile App / Client
       │
       ▼  HTTPS
┌──────────────────────────────────────┐
│         API Gateway (5000)           │
│  JWT Auth · Rate Limit · CORS        │
│  Health Dashboard · Correlation ID   │
└───┬────────┬────────┬────────┬───────┘
    │        │        │        │
    ▼        ▼        ▼        ▼
 Identity  YT DL    ImgTools  Scheduler
  5001     5002      5003      5004
```

## Port Table

| Port | Service | Protocol | Exposed | Purpose |
|------|---------|----------|---------|---------|
| **5000** | Gateway | HTTP/HTTPS | Yes | Single entry point. All client traffic goes here. |
| **5001** | Identity | HTTP | Internal | Auth: register, login, JWT, refresh tokens |
| **5002** | YtDownloader | HTTP | Internal | YouTube search, MP3 download, audio preview |
| **5003** | ImageTools | HTTP | Internal | Image processing (placeholder) |
| **5004** | Scheduler | HTTP | Internal | Weekend appointments, clients, expenses, reports |
| 5432 | PostgreSQL | TCP | Internal | Production database (Docker only) |

## Routing

The Gateway routes by URL prefix:

| Prefix | Destination | Service |
|--------|-------------|---------|
| `/api/v1/auth/*` | `http://identity:5001` | Identity |
| `/api/v1/yt/*` | `http://ytdownloader:5002` | YtDownloader |
| `/api/v1/imagetools/*` | `http://imagetools:5003` | ImageTools |
| `/api/v1/scheduler/*` | `http://scheduler:5004` | Scheduler |

## Development

In development, all services run directly on the host:

```bash
./backend/start-dev.sh   # starts all 5 services
./backend/stop-dev.sh    # stops all services

# Or individually:
dotnet run --project backend/src/JiApp.Identity   --urls http://0.0.0.0:5001
dotnet run --project backend/src/JiApp.YtDownloader --urls http://0.0.0.0:5002
dotnet run --project backend/src/JiApp.ImageTools  --urls http://0.0.0.0:5003
dotnet run --project backend/src/JiApp.Scheduler   --urls http://0.0.0.0:5004
dotnet run --project backend/src/JiApp.Gateway     --urls http://0.0.0.0:5000
```

Services are directly accessible on their ports — useful for debugging. The mobile app only talks to port 5000 (Gateway).

## Production

In production, all services run in Docker:

```bash
./backend/start-prod.sh  # docker compose up -d --build
docker compose down      # stop everything
```

Only the Gateway port (5000) is exposed to the host. Backend services communicate over the internal Docker network.
