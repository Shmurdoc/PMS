# SAFARIstack PMS - Backend API

Production-ready ASP.NET Core 8 Minimal API with Clean Architecture and Modular Monolith design.

## Architecture

### Modular Structure
- **Core**: Bookings, Guests, Properties (core business domain)
- **Modules.Staff**: RFID Hardware Integration, Attendance, BCEA Labor Logic
- **Modules.Addons**: Extensibility for Energy Management, Safari Operations
- **Infrastructure**: EF Core, PostgreSQL, Caching, External Services
- **Shared**: Common DTOs, Utilities, Constants

### Key Features
- Clean Architecture with strict separation of concerns
- Event-driven communication via MediatR
- PostgreSQL with Supabase (Code-First EF Core)
- Strong typing with UUID identifiers
- Dual authentication: JWT (users) + X-Reader-API-Key (RFID hardware)
- SA-specific: ZAR currency, 15% VAT, 1% Tourism Levy
- Network resilience: Edge buffering for offline scenarios
- Security: Velocity checks, rate limiting

## Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL (Supabase account)
- Visual Studio 2022 / VS Code / Rider

### Configuration
1. Copy `appsettings.example.json` to `appsettings.Development.json`
2. Update Supabase connection string and API keys
3. Run migrations: `dotnet ef database update`

### Running
```bash
cd src/SAFARIstack.API
dotnet run
```

API will be available at: https://localhost:7001

### API Documentation
Swagger UI: https://localhost:7001/swagger

## Project Structure
```
SAFARIstack/
├── src/
│   ├── SAFARIstack.API/              # Minimal API endpoints
│   ├── SAFARIstack.Core/             # Core domain (Bookings, Guests, Properties)
│   ├── SAFARIstack.Modules.Staff/    # Staff & RFID module
│   ├── SAFARIstack.Modules.Addons/   # Extensibility module
│   ├── SAFARIstack.Infrastructure/   # Data access, external services
│   └── SAFARIstack.Shared/           # Common code
└── SAFARIstack.sln
```

## License
Proprietary - SAFARIstack PMS © 2026
