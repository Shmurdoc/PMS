# 🐳 SAFARIstack PMS — Docker Stack

Complete Docker setup for SAFARIstack Property Management System with all microservices orchestrated via Docker Compose.

## 📋 What's Included

| Service | Port | Technology | Purpose |
|---------|------|-----------|---------|
| **Backend API** | 8080 | .NET 9, ASP.NET Core | PMS core API, GraphQL, WebSocket |
| **Frontend** | 3000 | Blazor WASM, MudBlazor | Staff/Admin dashboard |
| **Portal** | 3001 | Blazor WASM, MudBlazor | Enterprise admin portal |
| **Guest PWA** | 3002 | Blazor WASM PWA | Guest-facing application |
| **Database** | 5432 | PostgreSQL 18 | Data persistence |

---

## 🚀 Quick Start (30 seconds)

### 1️⃣ Mac/Linux
```bash
# Setup
cp .env.example .env
nano .env  # Edit JWT_SECRET and POSTGRES_PASSWORD

# Run
./docker.sh up
```

### 2️⃣ Windows
```powershell
# Setup
copy .env.example .env
notepad .env  # Edit JWT_SECRET and POSTGRES_PASSWORD

# Run
.\docker.ps1 -Command up
```

### 3️⃣ Docker CLI (Any OS)
```bash
docker compose up -d
```

---

## ✅ Verify Installation

```bash
# Check all containers are running
docker compose ps

# Check API health
curl http://localhost:8080/health/live

# View logs
docker compose logs -f
```

**Expected output:**
```
CONTAINER ID   IMAGE              STATUS           PORTS
xxx            safaristack-db     Up (healthy)     5432
xxx            safaristack-api    Up (healthy)     8080
xxx            safaristack-fe     Up (healthy)     3000
xxx            safaristack-portal Up (healthy)     3001
xxx            safaristack-pwa    Up (healthy)     3002
```

---

## 🌐 Access Applications

- **Frontend (Staff/Admin):** http://localhost:3000 → Sign in with admin credentials
- **Portal (Enterprise):** http://localhost:3001 → Multi-property management
- **Guest PWA:** http://localhost:3002 → Guest downloads booking
- **API Docs:** http://localhost:8080/swagger
- **Database (psql):** `localhost:5432` → safaristack/[password]

---

## 📝 Management Commands

### Mac/Linux
```bash
./docker.sh up              # 🟢 Start all services
./docker.sh down            # 🔴 Stop all services
./docker.sh status          # 📊 Show service health
./docker.sh logs            # 📋 View all logs
./docker.sh logs api        # 📋 View API logs only
./docker.sh restart api     # 🔄 Restart API service
./docker.sh rebuild         # 🔨 Rebuild all images
./docker.sh backup          # 💾 Backup database
./docker.sh shell api       # 🖥️ SSH into API container
./docker.sh clean           # 🗑️ Remove all (data loss!)
```

### Windows
```powershell
.\docker.ps1 -Command up              # 🟢 Start all services
.\docker.ps1 -Command down            # 🔴 Stop all services
.\docker.ps1 -Command status          # 📊 Show service health
.\docker.ps1 -Command logs            # 📋 View all logs
.\docker.ps1 -Command logs -Service safaristack-api  # 📋 View API logs
.\docker.ps1 -Command restart -Service safaristack-api  # 🔄 Restart
.\docker.ps1 -Command rebuild         # 🔨 Rebuild all images
.\docker.ps1 -Command backup          # 💾 Backup database
.\docker.ps1 -Command clean           # 🗑️ Remove all (data loss!)
```

### Docker CLI (Any OS)
```bash
docker compose up -d                              # Start
docker compose down                               # Stop
docker compose ps                                 # Status
docker compose logs -f --tail 50 safaristack-api # Logs
docker compose restart safaristack-api            # Restart
docker compose build --no-cache                   # Rebuild
docker compose down -v                            # Clean (⚠️ data loss)
```

---

## 🔧 Configuration

### Environment Variables (`.env`)

**Required:**
```env
POSTGRES_PASSWORD=YourStrongPassword123!
JWT_SECRET=GeneratedUsing-openssl-rand-base64-32
```

**Optional:**
```env
POSTGRES_DB=safaristack
POSTGRES_USER=safaristack
POSTGRES_PORT=5432
API_PORT=8080
```

### Generate Strong Secrets

**Linux/Mac:**
```bash
openssl rand -base64 32  # JWT_SECRET
```

**Windows PowerShell:**
```powershell
[System.Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Maximum 256) }))
```

---

## 🛠️ Common Tasks

### Update Code & Rebuild

```bash
# Pull latest code (from git)
git pull

# Rebuild affected services
docker compose build --no-cache safaristack-frontend
docker compose up -d              # Restart with new image

# Or rebuild everything
docker compose build --no-cache
docker compose up -d
```

### Database Management

```bash
# Connect to PostgreSQL
docker exec -it safaristack-db psql -U safaristack -d safaristack

# Common queries inside psql
\dt                          # List tables
SELECT version();            # Check PG version
SELECT COUNT(*) FROM bookings;  # Count records
\q                           # Exit

# Backup database
docker exec safaristack-db pg_dump -U safaristack safaristack > backup.sql

# Restore from backup
cat backup.sql | docker exec -i safaristack-db psql -U safaristack safaristack
```

### View Real-Time Logs

```bash
# All services
docker compose logs -f

# Specific service (last 100 lines)
docker compose logs -f --tail 100 safaristack-api

# Follow frontend logs
docker compose logs -f safaristack-frontend

# Grep for errors
docker compose logs safaristack-api | grep ERROR
```

### Network Troubleshooting

```bash
# Check if services can reach each other
docker exec safaristack-frontend ping api

# Test API from frontend container
docker exec safaristack-frontend wget -O- http://api:8080/health/live

# List all containers and their networks
docker network inspect safaristack_safaristack-network
```

---

## ⚠️ Troubleshooting

### Containers Won't Start

**Check logs:**
```bash
docker compose logs
```

**Common causes:**
- ❌ Port in use: `docker lsof -i :3000` or `netstat -ano`
- ❌ Missing `.env`: `cp .env.example .env`
- ❌ Insufficient resources: Increase Docker Desktop memory
- ❌ Database won't start: Check `POSTGRES_PASSWORD` format (special chars?)

### Frontend Shows Blank Page

```bash
# Check service health
docker compose ps

# Review frontend logs
docker compose logs safaristack-frontend

# Test API connectivity from frontend
docker exec safaristack-frontend curl http://api:8080/health/live

# Check if nginx is serving files
docker exec safaristack-frontend ls -la /var/www/html
```

### High Memory Usage

```bash
# Check resource usage per container
docker stats

# Reduce limits in docker-compose.yml or restart Docker
docker system prune -a  # Clean up unused resources
```

### Database Connection Failed

```bash
# Verify database is healthy
docker compose ps postgres

# Check database logs
docker compose logs postgres

# Manually test connection
docker exec safaristack-db pg_isready -U safaristack
```

---

## 🔒 Security Checklist

Before going production:

- [ ] Change `POSTGRES_PASSWORD` to strong, random value
- [ ] Change `JWT_SECRET` to strong, random value  
- [ ] Set up HTTPS with reverse proxy (nginx, Traefik)
- [ ] Enable Docker resource limits (memory, CPU)
- [ ] Configure firewall to only expose needed ports
- [ ] Set up automated database backups
- [ ] Enable container autorestart: `restart: unless-stopped`
- [ ] Run `docker system prune -a` weekly
- [ ] Keep base images updated
- [ ] Monitor logs for errors/warnings

---

## 📊 Resource Requirements

### Minimum
- **CPU:** 2 cores
- **RAM:** 4 GB
- **Disk:** 20 GB
- **Ports:** 3000, 3001, 3002, 5432, 8080

### Recommended
- **CPU:** 4 cores
- **RAM:** 8 GB
- **Disk:** 50+ GB (for backups)
- SSD recommended

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│                     Docker Host                         │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │       safaristack-network (bridge)               │  │
│  │                                                   │  │
│  │  ┌─────────────────────────────────────────┐    │  │
│  │  │  Frontend                               │    │  │
│  │  │  (Blazor WASM + nginx)  :3000           │    │  │
│  │  └─────────────────────────────────────────┘    │  │
│  │                    ↓                             │  │  
│  │  ┌─────────────────────────────────────────┐    │  │
│  │  │  Portal                                 │    │  │
│  │  │  (Blazor WASM + nginx)  :3001           │    │  │
│  │  └─────────────────────────────────────────┘    │  │
│  │                    ↓                             │  │
│  │  ┌─────────────────────────────────────────┐    │  │
│  │  │  Guest PWA                              │    │  │
│  │  │  (Blazor WASM + nginx)  :3002           │    │  │
│  │  └─────────────────────────────────────────┘    │  │
│  │                    ↓                             │  │
│  │  ┌─────────────────────────────────────────┐    │  │
│  │  │  API                                    │    │  │
│  │  │  (.NET 9 + ASP.NET Core)  :8080         │    │  │
│  │  └─────────────────────────────────────────┘    │  │
│  │                    ↓                             │  │
│  │  ┌─────────────────────────────────────────┐    │  │
│  │  │  Database                               │    │  │
│  │  │  (PostgreSQL 18)        :5432           │    │  │
│  │  └─────────────────────────────────────────┘    │  │
│  │                                                   │  │
│  └──────────────────────────────────────────────────┘  │
│                                                        │
└─────────────────────────────────────────────────────────┘
```

---

## 📚 Additional Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [PostgreSQL Docker Image](https://hub.docker.com/_/postgres)
- [nginx Docker Image](https://hub.docker.com/_/nginx)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [Docker Networking Guide](https://docs.docker.com/config/containers/container-networking/)

---

## 🆘 Get Help

1. **Check logs first:**
   ```bash
   docker compose logs -f
   ```

2. **Verify service health:**
   ```bash
   docker compose ps
   ```

3. **Test connectivity:**
   ```bash
   docker exec safaristack-frontend curl http://api:8080/health/live
   ```

4. **Review comprehensive guide:**
   ```bash
   cat DOCKER-DEPLOYMENT.md
   ```

---

**Status:** ✅ Production Ready | **Last Updated:** Feb 2026 | **Version:** 1.0

