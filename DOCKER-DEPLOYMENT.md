# SAFARIstack PMS — Docker Deployment Guide

## Quick Start

### 1. Prerequisites
- Docker Desktop (22.0+) with Docker Compose (2.0+)
- Minimum resources: 4GB RAM, 2 CPU cores, 20GB disk space
- Ports available: 3000, 3001, 3002, 5432, 8080

### 2. Configuration

Copy `.env.example` to `.env` and update the values:
```bash
cd backend
cp .env.example .env
# Edit .env with your settings (especially JWT_SECRET, POSTGRES_PASSWORD)
```

**Important**: Generate a strong JWT_SECRET:
```bash
# Linux/Mac
openssl rand -base64 32

# Windows PowerShell
[System.Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Maximum 256) }))
```

### 3. Start the Stack

```bash
cd backend
docker compose up -d
```

**Output:**
```
✓ safaristack-db       (PostgreSQL 18)
✓ safaristack-api       (API on :8080)
✓ safaristack-frontend  (Frontend on :3000)
✓ safaristack-portal    (Portal on :3001)
✓ safaristack-guest-pwa (PWA on :3002)
```

### 4. Access Applications

| Application | URL | Purpose |
|---|---|---|
| **Frontend** | http://localhost:3000 | Staff/Admin Interface |
| **Portal** | http://localhost:3001 | Admin Portal |
| **Guest PWA** | http://localhost:3002 | Guest-facing App |
| **API** | http://localhost:8080 | Backend API |
| **Database** | localhost:5432 | PostgreSQL 18 |

### 5. Verify Health

```bash
# Check all containers
docker ps

# Check container logs
docker logs safaristack-api
docker logs safaristack-frontend
docker logs safaristack-portal
docker logs safaristack-guest-pwa

# Test API health
curl http://localhost:8080/health/live
```

---

## Management Commands

### Stop All Services
```bash
docker compose down
```

### Stop and Remove Data (⚠️ deletes database)
```bash
docker compose down -v
```

### Restart a Service
```bash
docker compose restart safaristack-api
# Or: docker compose restart safaristack-frontend
```

### View Logs
```bash
# All services
docker compose logs -f

# Specific service (last 50 lines, follow)
docker compose logs -f --tail 50 safaristack-api
```

### Rebuild After Code Changes
```bash
# Rebuild all images
docker compose build --no-cache

# Rebuild specific service
docker compose build --no-cache safaristack-frontend

# Rebuild and restart
docker compose up --build -d
```

### Database Management

#### Connect to PostgreSQL
```bash
docker exec -it safaristack-db psql -U safaristack -d safaristack
```

#### Backup Database
```bash
docker exec safaristack-db \
  pg_dump -U safaristack safaristack > backup.sql
```

#### Restore Database
```bash
docker exec -i safaristack-db \
  psql -U safaristack safaristack < backup.sql
```

---

## Troubleshooting

### Containers Won't Start

**Check logs:**
```bash
docker compose logs
```

**Common issues:**
- Port already in use: `lsof -i :3000` or `netstat -ano`
- Insufficient resources: Check Docker Desktop memory/CPU allocation
- Missing .env file: Copy `.env.example` to `.env`

### API Can't Connect to Database
```bash
# Verify database is healthy
docker compose ps
# Status should show "healthy"

# Check API logs for connection errors
docker compose logs safaristack-api
```

### Frontend Shows Blank Page
```bash
# Check browser console for errors (F12)
# Verify API is accessible from frontend container:
docker exec safaristack-frontend wget -O- http://api:8080/health/live

# Check nginx logs
docker compose logs safaristack-frontend
```

### High Disk Usage

```bash
# Clean up Docker resources
docker system prune -a

# Remove unused volumes
docker volume prune
```

---

## Performance Tuning

### Increase Resource Limits

Edit `docker-compose.yml`:
```yaml
deploy:
  resources:
    limits:
      memory: 1G    # Increase from 512M
      cpus: "2.0"   # Increase from 1.0
```

### Database Optimization

```bash
# Connect to PostgreSQL
docker exec -it safaristack-db psql -U safaristack -d safaristack

# Run maintenance
VACUUM ANALYZE;
REINDEX DATABASE safaristack;
```

---

## Security Notes

### 🔐 Before Production Deployment

1. **Change default credentials:**
   - POSTGRES_PASSWORD
   - JWT_SECRET
   - API administrator account password

2. **Configure HTTPS:**
   - Set up reverse proxy (nginx/Traefik)
   - Use Let's Encrypt for SSL certificates

3. **Database backups:**
   - Set up automated backup strategy
   - Store backups off-site

4. **Network security:**
   - Don't expose ports directly to internet
   - Use VPN or firewall
   - Configure Docker firewall rules

5. **Regular updates:**
   ```bash
   # Update base images
   docker pull postgres:18-alpine
   docker pull nginx:alpine
   docker pull mcr.microsoft.com/dotnet/aspnet:9.0-alpine
   
   # Rebuild and restart
   docker compose build --no-cache
   docker compose up -d
   ```

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Docker Host                          │
├─────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────┐   │
│  │       safaristack-network (bridge)               │   │
│  │  ┌──────────────────────────────────────────┐    │   │
│  │  │  Frontend (nginx)      :3000             │    │   │
│  │  │  Portal (nginx)        :3001             │    │   │
│  │  │  Guest PWA (nginx)     :3002             │    │   │
│  │  │  API (.NET 9)          :8080             │    │   │
│  │  │  Database (PG 18)      :5432             │    │   │
│  │  └──────────────────────────────────────────┘    │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
         ↓
    Host Ports
    3000, 3001, 3002, 8080, 5432
```

---

## Environment Variables Reference

### Required
- `POSTGRES_PASSWORD` — Database password (must be strong)
- `JWT_SECRET` — JWT signing key (32+ characters)

### Optional
- `POSTGRES_DB` — Database name (default: safaristack)
- `POSTGRES_USER` — Database user (default: safaristack)
- `POSTGRES_PORT` — Database port (default: 5432)
- `API_PORT` — API port (default: 8080)

---

## Getting Help

1. Check logs: `docker compose logs -f`
2. Review health: `docker compose ps`
3. Test connectivity: `docker exec safaristack-frontend curl http://api:8080/health/live`

