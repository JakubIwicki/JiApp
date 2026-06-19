# Cloudflare Tunnel — Future TLS Alternative

Side option noted for future if the baked CA cert approach becomes inconvenient.

## Setup

1. Acquire a domain (or use a free Freenom .tk/.ml/.cf domain)
2. In Cloudflare Zero Trust dashboard:
   - Create a tunnel: `cloudflared tunnel create jiapp`
   - Point it at `http://gateway:6700`
   - Get a CNAME record
3. Add `cloudflared` to Docker Compose:
   ```yaml
   cloudflared:
     image: cloudflare/cloudflared:latest
     command: tunnel --no-autoupdate run --token ${CF_TUNNEL_TOKEN}
     restart: unless-stopped
   ```
4. Add `CF_TUNNEL_TOKEN` to `/opt/jiapp/.env`
5. Gateway listens on HTTP (Cloudflare terminates TLS)
6. Mobile app connects to `https://jiapp.<your-domain>` (Cloudflare edge)

## Benefits

- **Zero open ports** — no inbound security group rules needed
- **Automatic TLS** — Cloudflare handles edge certificate + renewal
- **Free** — unlimited tunnels, no bandwidth caps
- **No cert renewal** — no APK rebuilds for TLS

## Trade-offs

- Dependency on Cloudflare (external service)
- Traffic passes through Cloudflare's network
- Need a domain name (can use free TLD)
- Adds one more container to Docker Compose

## Migration Path from Baked CA

1. Provision domain + Cloudflare Tunnel
2. Add `CF_TUNNEL_TOKEN` to `/opt/jiapp/.env`
3. Add `cloudflared` to `docker-compose.prod.yml`
4. Update mobile `API_BASE_URL` in `.env`
5. Build new APK
6. Remove cert volume mount from Gateway (optional — can keep both)
7. **Inbound port 6700 can be closed** in security group

**Cost:** $0 (Cloudflare free tier) + domain registration (optional, ~$12/year or free Freenom TLD).
