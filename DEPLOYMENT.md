# SAFARIstack PMS - Deployment Guide

## Prerequisites
- .NET 9 SDK
- PostgreSQL 15+ (Supabase account)
- Docker (optional, for containerization)

## Database Setup

### 1. Supabase Configuration
1. Create a new project at https://supabase.com
2. Navigate to Settings → Database
3. Copy your connection string
4. Update `appsettings.json` with your connection string

### 2. Run Migrations
```bash
cd src/SAFARIstack.API
dotnet ef migrations add InitialCreate --project ../SAFARIstack.Infrastructure
dotnet ef database update --project ../SAFARIstack.Infrastructure
```

## Local Development

### 1. Install Dependencies
```bash
dotnet restore
```

### 2. Build Solution
```bash
dotnet build
```

### 3. Run API
```bash
cd src/SAFARIstack.API
dotnet run
```

API will be available at: https://localhost:7001

### 4. Access Swagger
Navigate to: https://localhost:7001/swagger

## RFID Hardware Setup

### 1. Register RFID Reader
```http
POST /api/rfid/register
Content-Type: application/json

{
  "propertyId": "your-property-guid",
  "readerSerial": "READER-001",
  "readerName": "Main Entrance",
  "readerType": "Fixed",
  "apiKey": "your-generated-api-key"
}
```

### 2. Configure Reader
- Set reader to POST to: `https://your-api.com/api/rfid/check-in`
- Add header: `X-Reader-API-Key: your-api-key`
- Payload format:
```json
{
  "cardUid": "ABC123DEF456",
  "readerId": "reader-guid"
}
```

## Production Deployment

### Azure Deployment
```bash
# Build for production
dotnet publish -c Release -o ./publish

# Deploy to Azure App Service
az webapp deployment source config-zip \
  --resource-group safaristack-rg \
  --name safaristack-api \
  --src publish.zip
```

### Docker Deployment
```bash
# Build Docker image
docker build -t safaristack-api:latest .

# Run container
docker run -d \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="your-connection-string" \
  --name safaristack-api \
  safaristack-api:latest
```

## Environment Variables

Required environment variables:
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string
- `JwtSettings__SecretKey`: JWT signing key (minimum 32 characters)
- `JwtSettings__Issuer`: JWT issuer
- `JwtSettings__Audience`: JWT audience

Optional:
- `RfidAuthentication__EnableIpWhitelist`: Enable IP whitelisting for RFID readers
- `EdgeBuffer__Enabled`: Enable offline edge buffering

## Security Checklist

- [ ] Change default JWT secret key
- [ ] Enable HTTPS in production
- [ ] Configure CORS for specific origins
- [ ] Set up firewall rules
- [ ] Enable rate limiting
- [ ] Configure IP whitelisting for RFID readers
- [ ] Set up monitoring and alerting
- [ ] Enable audit logging
- [ ] Regular database backups

## Monitoring

### Health Check
```bash
curl https://your-api.com/health
```

### Logs
Logs are written to:
- Console (stdout)
- File: `logs/safaristack-{date}.log`

Configure log aggregation (Application Insights, Sentry, etc.) in production.

## Support
For issues or questions, contact: support@safaristack.com
