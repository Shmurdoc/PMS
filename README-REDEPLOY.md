Redeploy script

Quick steps to rebuild and redeploy SAFARIstack services (Windows PowerShell):

1. Open PowerShell in:
   `c:\Users\madoc\OneDrive\Documents\Lodge&Hotel\backend\scripts`

2. Run:
```powershell
.\redeploy.ps1
```

What it does:
- Runs `docker compose build --no-cache` for `frontend`, `portal`, `guest-pwa`, and `api`.
- Runs `docker compose up -d --force-recreate --remove-orphans` to recreate containers.
- Prints container status when finished.

Notes:
- Ensure your environment variables (in `.env` or your shell) include required secrets like `POSTGRES_PASSWORD` and `JWT_SECRET`.
- On first run this will take several minutes while images are built.