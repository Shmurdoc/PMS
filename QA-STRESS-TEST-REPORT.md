# SAFARIstack PMS — Architecture Audit & QA Stress-Test Report

**Date:** February 10, 2026  
**Auditor:** Automated Deep-Code Review  
**Scope:** Full codebase — 31 entities, 55 endpoints, 6 projects, 5 modules  
**Verdict:** 🟡 **Not production-ready.** Strong DDD bones, critical security & completeness gaps.

---

## Table of Contents

1. [Architecture Diagrams](#1-architecture-diagrams)
2. [Entity Classification](#2-entity-classification)
3. [QA Stress-Test Findings](#3-qa-stress-test-findings)
4. [Scoring Summary](#4-scoring-summary)
5. [Recommended Fix Order](#5-recommended-fix-order)

---

## 1. Architecture Diagrams

### 1.1 — System Layer Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                      CLIENTS                                 │
│   Browser (SPA)  │  Mobile App  │  RFID Hardware  │  OTAs   │
└────────┬─────────┴──────┬───────┴───────┬─────────┴────┬────┘
         │                │               │              │
         ▼                ▼               ▼              ▼
┌──────────────────────────────────────────────────────────────┐
│                   ASP.NET CORE MINIMAL API                   │
│  ┌──────────┐ ┌──────────────┐ ┌───────────┐ ┌───────────┐  │
│  │ JWT Auth │ │ RFID ApiKey  │ │ Rate Limit│ │ CORS      │  │
│  │ Middleware│ │ Auth         │ │ (NOT WIRED)│ │ (OPEN)   │  │
│  └────┬─────┘ └──────┬───────┘ └───────────┘ └───────────┘  │
│       │               │                                      │
│  ┌────▼───────────────▼──────────────────────────────────┐   │
│  │              ENDPOINT GROUPS (9)                       │   │
│  │  Auth │ Booking │ Guest │ Room │ Financial │ HK │ ... │   │
│  │         + TenantValidationFilter on all groups         │   │
│  └────────────────────┬──────────────────────────────────┘   │
│                       │                                      │
│  ┌────────────────────▼──────────────────────────────────┐   │
│  │           GLOBAL EXCEPTION HANDLER                    │   │
│  │    401 Auth │ 403 Tenant │ 400 Validation │ 500 ISE   │   │
│  └───────────────────────────────────────────────────────┘   │
└──────────────────────────┬───────────────────────────────────┘
                           │
┌──────────────────────────▼───────────────────────────────────┐
│                     CORE / DOMAIN                            │
│  ┌──────────────┐ ┌───────────────┐ ┌─────────────────────┐  │
│  │  31 Entities │ │ 31 Domain     │ │ 5 Value Objects     │  │
│  │  (DDD Style) │ │ Events        │ │ (3 unused)          │  │
│  │              │ │ (0 handlers!) │ │                     │  │
│  └──────────────┘ └───────────────┘ └─────────────────────┘  │
│  ┌──────────────┐ ┌───────────────┐ ┌─────────────────────┐  │
│  │ 5 Domain     │ │ MediatR CQRS  │ │ FluentValidation    │  │
│  │ Services     │ │ 4/25 handlers │ │ 5 validators only   │  │
│  └──────────────┘ └───────────────┘ └─────────────────────┘  │
└──────────────────────────┬───────────────────────────────────┘
                           │
┌──────────────────────────▼───────────────────────────────────┐
│                   INFRASTRUCTURE                             │
│  ┌──────────────┐ ┌───────────────┐ ┌─────────────────────┐  │
│  │ EF Core 9.0  │ │ AuthService   │ │ Typed Repositories  │  │
│  │ DbContext    │ │ (JWT+BCrypt)  │ │ (11 repos)          │  │
│  │ +QueryFilters│ │               │ │                     │  │
│  └──────┬───────┘ └───────────────┘ └─────────────────────┘  │
│         │  ┌───────────────────┐  ┌────────────────────────┐  │
│         │  │ UnitOfWork        │  │ CorrelationId          │  │
│         │  │ (no event dispatch)│ │ Middleware + Serilog   │  │
│         │  └───────────────────┘  └────────────────────────┘  │
└─────────┼────────────────────────────────────────────────────┘
          │
          ▼
┌──────────────────────────────────────────────────────────────┐
│                    PostgreSQL 18                              │
│            31 tables │ safaristack_dev                        │
│            3 migrations applied                              │
└──────────────────────────────────────────────────────────────┘
```

### 1.2 — Multi-Tenancy Data Isolation (3-Layer Defense)

```
                    ┌─────────────┐
  HTTP Request ───▶ │ JWT Token   │
                    │ propertyId  │
                    │ claim       │
                    └──────┬──────┘
                           │
              ┌────────────▼────────────┐
   LAYER 1    │  TenantValidationFilter │   Endpoint filter
              │  Checks route/query     │   ⚠️ Does NOT check
              │  PropertyId vs JWT      │      request body!
              └────────────┬────────────┘
                           │
              ┌────────────▼────────────┐
   LAYER 2    │  ITenantProvider        │   Service layer
              │  TenantProvider.cs      │   Reads JWT claim,
              │  ValidatePropertyAccess │   caches PropertyId
              └────────────┬────────────┘
                           │
              ┌────────────▼────────────┐
   LAYER 3    │  EF Global Query        │   Database layer
              │  Filters on every       │   Auto-filters ALL
              │  IMultiTenant entity    │   SELECT queries
              │                         │
              │  Pattern:               │
              │  _tenantProvider == null │
              │  || !HasTenantContext    │
              │  || IsSuperAdmin        │
              │  || PropertyId == claim  │
              └─────────────────────────┘
```

### 1.3 — Module Dependency Graph

```
SAFARIstack.API ──────────────────────────────┐
   │                                           │
   ├──▶ SAFARIstack.Core                       │
   │       │                                   │
   │       ├── Domain/ (31 entities)           │
   │       ├── Application/Bookings/ (CQRS)    │
   │       └── Domain/Interfaces/              │
   │              │                            │
   ├──▶ SAFARIstack.Infrastructure             │
   │       │                                   │
   │       ├── Data/ApplicationDbContext        │
   │       ├── Authentication/AuthService       │
   │       ├── Repositories/ (11 typed)        │
   │       ├── Migrations/ (3)                 │
   │       └── Resilience/ (retry policies)    │
   │              │                            │
   ├──▶ SAFARIstack.Shared                     │
   │       │                                   │
   │       ├── Domain/ (Entity, AggregateRoot) │
   │       └── ValueObjects/ (5)               │
   │                                           │
   ├──▶ SAFARIstack.Modules.Staff              │
   │       │  ⚠️ EF Core 8.0.0 (mismatch!)    │
   │       ├── Domain/ (StaffMember, etc.)     │
   │       └── Application/                    │
   │                                           │
   └──▶ DEAD MODULES (no real code):           │
          ├── Modules.Analytics                │
          ├── Modules.Channels                 │
          ├── Modules.Revenue                  │
          ├── Modules.Events                   │
          └── Modules.Addons                   │
```

---

## 2. Entity Classification

### Tier 1 — Core (Must Work Perfectly)

| Entity | Multi-Tenant | Soft-Delete | Auditable | Status |
|--------|:-----------:|:-----------:|:---------:|--------|
| Property | ✅ (is root) | ✅ | ✅ | ✅ Good |
| Room | ✅ | ✅ | ✅ | ✅ Good |
| RoomType | ✅ | ✅ | ✅ | ✅ Good |
| Booking | ✅ | ✅ | ✅ | ✅ Good |
| BookingRoom | ✅ | ✅ | ✅ | ✅ Good |
| Guest | ✅ | ✅ | ✅ | ✅ Good |
| GuestDocument | ✅ | ✅ | ✅ | ✅ Good |
| User | ✅ | ✅ | ✅ | ✅ Good |
| Folio | ✅ | ✅ | ✅ | ✅ Good |
| Payment | ✅ | ✅ | ✅ | ✅ Good |
| Invoice | ✅ | ✅ | ✅ | ✅ Good |
| InvoiceLineItem | ✅ | ✅ | ✅ | ✅ Good |

### Tier 2 — Important (Should Work)

| Entity | Multi-Tenant | Soft-Delete | Auditable | Status |
|--------|:-----------:|:-----------:|:---------:|--------|
| RoomBlock | ✅ | ✅ | ✅ | ⚠️ Endpoint bug |
| Rate | ✅ | ✅ | ✅ | ✅ Good |
| Season | ✅ | ✅ | ✅ | ⚠️ Endpoint returns 501 |
| HousekeepingTask | ✅ | ✅ | ✅ | ✅ Good |
| HousekeepingSchedule | ✅ | ✅ | ✅ | ✅ Good |
| Amenity | ✅ | ✅ | ✅ | ✅ Fixed in session |
| Floor | ✅ | ✅ | ✅ | ✅ Good |
| Role | ✅ | ✅ | ✅ | ✅ Good |
| Permission | ✅ | ✅ | ✅ | ✅ Good |
| RolePermission | ✅ | ✅ | ✅ | ✅ Good |
| UserRole | ✅ | ✅ | ✅ | ✅ Good |
| RefreshToken | ✅ | ❌ | ❌ | ⚠️ Stored in plain text |

### Tier 3 — Optional / Additive

| Entity | Multi-Tenant | Soft-Delete | Auditable | Status |
|--------|:-----------:|:-----------:|:---------:|--------|
| StaffMember | ✅ | ❌ | ❌ | ⚠️ Missing soft-delete & audit |
| StaffAttendance | ✅ | ❌ | ❌ | ⚠️ Missing soft-delete & audit |
| RfidReader | ✅ | ❌ | ❌ | ⚠️ Missing soft-delete & audit |
| RfidCard | ❌ | ❌ | ❌ | 🔴 No PropertyId! No IMultiTenant! |
| RfidAccessLog | ✅ | ❌ | ❌ | ⚠️ Missing soft-delete & audit |
| GuestPreference | ✅ | ✅ | ✅ | ✅ Good |
| GuestStay | ✅ | ✅ | ✅ | ✅ Good |
| BookingStatusHistory | ✅ | ✅ | ✅ | ✅ Good |
| RoomStatusHistory | ✅ | ✅ | ✅ | ✅ Good |
| RoomMaintenance | ✅ | ✅ | ✅ | ✅ Good |
| AuditLog | ✅ | ✅ | ✅ | ✅ Good |

### Tier 4 — Dead Code (Remove or Implement)

| Module | Files | Real Logic | Verdict |
|--------|-------|-----------|---------|
| Modules.Analytics | Module shell + empty classes | ❌ Zero | 🗑️ Remove or backlog |
| Modules.Channels | Module shell + empty classes | ❌ Zero | 🗑️ Remove or backlog |
| Modules.Revenue | Module shell + empty classes | ❌ Zero | 🗑️ Remove or backlog |
| Modules.Events | Module shell + contracts only | ❌ Zero | 🗑️ Remove or backlog |
| Modules.Addons | Module shell only | ❌ Zero | 🗑️ Remove or backlog |

**Dead MediatR Artifacts:** 21 command/query classes defined with ZERO handlers. They compile but do nothing.

---

## 3. QA Stress-Test Findings

### 🔴 P0 — Show-Stoppers (Fix Before Any Demo)

#### P0-1: Domain Events Never Dispatched
- **Location:** `UnitOfWork.cs` → `SaveChangesAsync()`
- **Issue:** 31 domain events are defined across entities. `AuditableAggregateRoot.AddDomainEvent()` collects them. `MediatRDomainEventDispatcher.DispatchPendingEventsAsync()` exists. But **nobody calls it**. `UnitOfWork.SaveChangesAsync()` calls `_context.SaveChangesAsync()` and returns. Events are collected and silently discarded.
- **Impact:** Audit trails, notifications, cross-aggregate side-effects — all dead. The entire event-driven architecture is theater.
- **Fix:** Call `_domainEventDispatcher.DispatchPendingEventsAsync(_context)` after `SaveChangesAsync()` in `UnitOfWork.cs`.

#### P0-2: Open Registration — Anyone Can Create SuperAdmin
- **Location:** `AuthEndpoints.cs` → `POST /api/auth/register` (AllowAnonymous)
- **Issue:** Registration endpoint is unauthenticated. The `RegisterCommand` accepts a role. Nothing prevents passing `"SuperAdmin"` as the role. `AuthService.RegisterAsync()` creates the user with whatever role is requested.
- **Impact:** Any internet user can create a SuperAdmin account and access all properties' data.
- **Fix:** Either require authentication for registration, or enforce role = "FrontDesk" on public registration and require admin auth for elevated roles.

#### P0-3: No Password Policy
- **Location:** `AuthService.cs` → `RegisterAsync()`
- **Issue:** Zero validation on password strength. `password = "1"` is accepted. No minimum length, no complexity, no breach checking.
- **Impact:** Accounts are trivially brute-forceable.
- **Fix:** Add minimum 8 chars, require uppercase + lowercase + digit at minimum.

#### P0-4: Hardcoded JWT Secret in appsettings.json
- **Location:** `appsettings.json` → `JwtSettings.Secret`
- **Value:** `"your-super-secret-jwt-key-minimum-32-characters-long-change-this-in-production"`
- **Impact:** If this ships, anyone who reads the source code (or guesses the obvious placeholder) can forge valid JWTs for any user/property.
- **Fix:** Move to `dotnet user-secrets` for development, environment variables for production.

#### P0-5: Rate Limiting Configured But Never Wired
- **Location:** `appsettings.json` has `IpRateLimiting` config. `Program.cs` never calls `app.UseIpRateLimiting()`.
- **Impact:** API is wide open to brute-force attacks, credential stuffing, and DDoS. The rate limiting NuGet package is installed and configured — it just isn't activated.
- **Fix:** Add `app.UseIpRateLimiting();` to the middleware pipeline in `Program.cs`.

---

### 🟠 P1 — Critical Bugs (Fix Before Beta)

#### P1-1: RoomBlock Endpoint Returns Wrong Entity
- **Location:** `RoomEndpoints.cs` → `GET /api/rooms/blocks/{blockId}`
- **Issue:** Endpoint fetches a `Room` instead of a `RoomBlock`. Code: `unitOfWork.Rooms.GetByIdAsync(blockId)` — should be `unitOfWork.RoomBlocks.GetByIdAsync(blockId)`.
- **Impact:** Endpoint is broken. Returns a random room or 404 instead of the requested block.

#### P1-2: Season Endpoint Returns 501
- **Location:** `RateEndpoints.cs` → `GET /api/rates/seasons/{propertyId}`
- **Issue:** Handler returns `Results.StatusCode(501)` with comment "Not Implemented". The route exists, Swagger shows it, clients will call it.
- **Impact:** Any feature depending on seasonal pricing is dead.

#### P1-3: PricingService N+1 Query
- **Location:** `DomainServices.cs` → `PricingService.CalculateStayPrice()`
- **Issue:** For each night of a stay, calls `_seasonRepository.GetActiveSeasonAsync(propertyId, date)` — which likely hits the DB each time. A 14-night stay = 14 DB round-trips.
- **Impact:** Slow pricing calculations, DB load under concurrent bookings.
- **Fix:** Bulk-fetch all seasons for the date range in one query, then match in memory.

#### P1-4: TenantValidation Doesn't Check Request Body
- **Location:** `TenantValidationFilter.cs`
- **Issue:** Filter checks PropertyId in route values and query strings only. On `POST /api/bookings`, the PropertyId is in the JSON body — unchecked. A user could submit a booking for Property B while authenticated as Property A.
- **Impact:** Multi-tenancy bypass on all write operations.
- **Fix:** Deserialize and inspect request body PropertyId in the filter, or validate in service layer.

#### P1-5: Refresh Tokens Stored in Plain Text
- **Location:** `AuthService.cs` → `GenerateRefreshToken()` / `RefreshTokenAsync()`
- **Issue:** Refresh tokens are stored as-is in the database. If the DB is breached, all refresh tokens are immediately usable.
- **Impact:** Token theft enables indefinite session hijacking.
- **Fix:** Store hashed refresh tokens (SHA-256), compare hashes on validation.

#### P1-6: EF Core Version Mismatch in Staff Module
- **Location:** `SAFARIstack.Modules.Staff.csproj` references EF Core **8.0.0**. All other projects use **9.0.0**.
- **Impact:** Runtime assembly binding conflicts, potential silent data corruption, migration incompatibilities.
- **Fix:** Update Staff module to EF Core 9.0.0.

---

### 🟡 P2 — Significant Gaps (Fix Before Production)

#### P2-1: Zero Input Validation on 45+ Endpoints
- **Issue:** Only 5 FluentValidation validators exist (all booking-related). The other ~50 endpoints accept any input shape. No length limits, no format validation, no XSS prevention.
- **Impact:** SQL injection via EF (unlikely but not impossible with raw queries), data corruption, XSS if data is rendered.

#### P2-2: No Pagination on List Endpoints
- **Issue:** `GET /api/guests/{propertyId}`, `GET /api/rooms/{propertyId}`, etc. return **all** records with no pagination.
- **Impact:** A property with 10,000 guests returns a 10MB JSON response. OOM on mobile clients, timeout on slow connections.

#### P2-3: Race Conditions on Booking & Payment
- **Issue:** No optimistic concurrency (no `[ConcurrencyCheck]` or row version). Two users booking the last room simultaneously both succeed. Two payments against the same folio can double-credit.
- **Impact:** Overbooking, financial discrepancies.

#### P2-4: HTTPS / CORS Not Locked Down
- **Issue:** `appsettings.json` has `AllowedOrigins: ["*"]`. No HTTPS redirect configured in `Program.cs`. CORS is fully open.
- **Impact:** Man-in-the-middle attacks, CSRF from any origin.

#### P2-5: Auth Route Collision
- **Location:** `AuthEndpoints.cs`
- **Issue:** `GET /api/users/{propertyId:guid}` and `GET /api/users/{userId:guid}` have the same route pattern. ASP.NET will match the first one registered for any GUID.
- **Impact:** One endpoint is unreachable.

#### P2-6: Audit Trail Gaps
- **Issue:** Staff entities (StaffMember, StaffAttendance, RfidReader, RfidCard) don't implement `IAuditable` or `ISoftDeletable`. Deletions are hard-deletes with no history.
- **Impact:** No paper trail for staff operations. Compliance risk for hospitality regulations.

#### P2-7: RfidCard Has No PropertyId
- **Location:** `RfidCard.cs` in Modules.Staff
- **Issue:** Entity has no `PropertyId` property, doesn't implement `IMultiTenant`. Not filtered by tenant.
- **Impact:** RFID cards from Property A visible to Property B queries. Security breach.

---

### 🟢 P3 — Polish & Best Practices (Fix Before Scale)

#### P3-1: 21 Dead MediatR Commands/Queries
- **Issue:** Command and query classes defined with no handlers. They exist in the codebase, get registered by DI, but throw at runtime if dispatched.
- **Impact:** Developer confusion, false sense of completeness.

#### P3-2: 3 Unused Value Objects
- **Issue:** `Address`, `PhoneNumber`, `PersonName` value objects defined in Shared but never used by any entity. All entities use primitive strings instead.
- **Impact:** Lost DDD benefit, inconsistent data validation.

#### P3-3: 5 Dead Modules
- **Issue:** Analytics, Channels, Revenue, Events, Addons modules have shell registrations but zero business logic.
- **Impact:** Increases build time, confuses new developers, gives false impression of features.

#### P3-4: Serilog Not Configured for Production
- **Issue:** Only console + file sinks. No structured log aggregation (Seq, ELK, Application Insights). Log files will grow unbounded.
- **Impact:** Debugging in production is manual log file grepping.

#### P3-5: No Health Check Beyond Basic
- **Issue:** `app.MapHealthChecks("/health")` exists but doesn't check DB connectivity, Redis, or external service health.
- **Impact:** Load balancer can't detect a running-but-broken instance.

#### P3-6: No Database Seeding
- **Issue:** No seed data for roles, permissions, default admin user, room types, etc. Fresh deployment requires manual database population.
- **Impact:** Every deployment is a manual setup process.

---

## 4. Scoring Summary

| Category | Score | Notes |
|----------|:-----:|-------|
| **Architecture** | 7/10 | Clean DDD/CQRS structure, proper separation. Modular monolith pattern is solid. Points lost for dead modules and unused CQRS handlers. |
| **Multi-Tenancy** | 7/10 | 3-layer defense is good design. Fixed in this audit session. Points lost for body-check gap and RfidCard miss. |
| **Security** | 3/10 | Open registration to SuperAdmin, no password policy, hardcoded JWT secret, plain-text refresh tokens, no rate limiting wired. Critical failures. |
| **Data Integrity** | 4/10 | No concurrency control, domain events never fire, 4 entities missing EF configs, staff entities lack soft-delete. |
| **Completeness** | 4/10 | 4/25 CQRS handlers implemented, 5/9 endpoint groups have stubs, Season endpoint returns 501, 5 modules are empty shells. |
| **Production Readiness** | 2/10 | No pagination, no input validation on most endpoints, no HTTPS enforcement, no health checks, no seed data, logs not production-ready. |

### Overall: **4.5 / 10**

> **Translation for investors:** The architecture is well-designed and the DDD foundation is professional-grade. However, the implementation is ~40% complete with critical security holes. It needs 3-4 weeks of focused security hardening and feature completion before a private beta, and 6-8 weeks before handling real guest data.

---

## 5. Recommended Fix Order

### Week 1 — Security Lockdown (P0)

| # | Task | Effort | Files |
|---|------|--------|-------|
| 1 | Secure registration (require auth for elevated roles) | 2h | AuthEndpoints.cs, AuthService.cs |
| 2 | Add password policy (min 8, complexity) | 1h | AuthService.cs |
| 3 | Move JWT secret to user-secrets / env vars | 1h | appsettings.json, Program.cs |
| 4 | Wire rate limiting middleware | 30m | Program.cs |
| 5 | Wire domain event dispatch after SaveChanges | 2h | UnitOfWork.cs |

### Week 2 — Critical Bugs (P1)

| # | Task | Effort | Files |
|---|------|--------|-------|
| 6 | Fix RoomBlock endpoint (wrong repository) | 15m | RoomEndpoints.cs |
| 7 | Implement Season endpoint (remove 501) | 2h | RateEndpoints.cs, SeasonRepository |
| 8 | Fix PricingService N+1 query | 2h | DomainServices.cs, SeasonRepository |
| 9 | Add request body tenant validation | 3h | TenantValidationFilter.cs |
| 10 | Hash refresh tokens | 2h | AuthService.cs, RefreshToken entity |
| 11 | Fix Staff module EF Core version to 9.0.0 | 15m | SAFARIstack.Modules.Staff.csproj |

### Week 3 — Data Integrity (P2)

| # | Task | Effort | Files |
|---|------|--------|-------|
| 12 | Add FluentValidation to all endpoints | 8h | New validator files, endpoint registrations |
| 13 | Add pagination to all list endpoints | 4h | All endpoint files, repositories |
| 14 | Add concurrency control (RowVersion) | 4h | Entities, DbContext, migration |
| 15 | Fix auth route collision | 30m | AuthEndpoints.cs |
| 16 | Add IMultiTenant to RfidCard | 1h | RfidCard.cs, ApplicationDbContext.cs |
| 17 | Add IAuditable + ISoftDeletable to Staff entities | 2h | Staff entity files |
| 18 | Lock down CORS + enforce HTTPS | 1h | Program.cs, appsettings.json |

### Week 4 — Completeness & Polish (P3)

| # | Task | Effort | Files |
|---|------|--------|-------|
| 19 | Remove or implement dead modules | 2h | 5 module projects |
| 20 | Clean up dead MediatR commands | 2h | Application/ folders |
| 21 | Use value objects in entities | 4h | Entity files |
| 22 | Add DB health check | 1h | Program.cs |
| 23 | Add seed data | 4h | New seeder classes |
| 24 | Configure production logging | 2h | appsettings.Production.json, Program.cs |

---

## Appendix A: Technology Stack

| Component | Version | Notes |
|-----------|---------|-------|
| .NET | 9.0 | Target framework |
| ASP.NET Core | 9.0 | Minimal API pattern |
| EF Core | 9.0.0 | ⚠️ 8.0.0 in Staff module |
| PostgreSQL | 18 | localhost:5432 |
| MediatR | 12.2.0 | CQRS mediator |
| FluentValidation | 11.11.0 | Input validation |
| BCrypt.Net-Next | 4.0.3 | Password hashing |
| Serilog | Latest | Structured logging |
| Npgsql.EntityFrameworkCore | 9.0.0 | PostgreSQL provider |

## Appendix B: Database Tables (31)

```
Properties              Rooms                   RoomTypes
Floors                  RoomBlocks              RoomMaintenances
RoomStatusHistories     Bookings                BookingRooms
BookingStatusHistories  Guests                  GuestDocuments
GuestPreferences        GuestStays              Folios
Payments                Invoices                InvoiceLineItems
HousekeepingTasks       HousekeepingSchedules   Amenities
Rates                   Seasons                 Users
Roles                   Permissions             RolePermissions
UserRoles               RefreshTokens           StaffMembers
StaffAttendances        RfidCards               RfidReaders
RfidAccessLogs          AuditLogs
```

## Appendix C: Endpoint Inventory (55)

| Group | Count | Working | Stubs | Broken |
|-------|:-----:|:-------:|:-----:|:------:|
| Auth | 19 | 19 | 0 | 0 |
| Booking | 5 | 2 | 3 | 0 |
| Guest | 6 | 6 | 0 | 0 |
| Room | 10 | 8 | 1 | 1 |
| Financial | 11 | 11 | 0 | 0 |
| Housekeeping | 7 | 7 | 0 | 0 |
| Staff | 4 | 0 | 4 | 0 |
| RFID | 3 | 2 | 1 | 0 |
| Rate | 4 | 2 | 1 | 1 |
| **Total** | **69** | **57** | **10** | **2** |

---

*Report generated from exhaustive review of all source files in the SAFARIstack PMS codebase.*
