# Day 29 — Foundation + Happy Path

**Branch:** `day28-adr`  
**Date:** 24 June 2026  
**Repo:** https://github.com/thinkbridge-thinkschool/MarbleCraftOMS.git

---

## Goal

A running system with one flow working against real infrastructure:  
`POST /api/v1/login → JWT → protected endpoint → 200`

---

## 1. Solution Structure (Clean / Onion Architecture)

```
src/
├── MarbleCraftOMS.Core                 ← Ring 1: entities + repository interfaces (zero deps)
│   ├── Entities/                       Product, Supplier, StockLot, AppUser, …
│   ├── Interfaces/                     IProductRepository, IInventoryRepository, …
│   └── Constants/                      Roles
│
├── MarbleCraftOMS.Application          ← Ring 2: use-case services + commands (depends on Core only)
│   ├── Auth/                           IAuthService, LoginCommand, LoginResponse
│   ├── Catalogue/                      IProductService, AddProductCommand, …
│   ├── Inventory/                      IInventoryService, AdjustStockCommand, …
│   └── Supplier/                       ISupplierService, AddSupplierCommand, …
│
├── MarbleCraftOMS.Infrastructure       ← Ring 3: EF Core + Dapper + repos (implements Core interfaces)
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/             EF fluent configs per entity
│   │   ├── Migrations/                 3 migrations (InitialCreate → Users)
│   │   ├── Repositories/               ProductRepository, InventoryRepository, …
│   │   └── DapperQueries/             ProductBrowseQuery, StockSummaryQuery
│   └── Services/                       MemoryCacheAdapter
│
├── MarbleCraftOMS.Api                  ← Ring 4: HTTP layer (depends on Application + Infrastructure)
│   ├── Controllers/                    Auth, Products, Suppliers, Inventory
│   ├── Data/                           DbInitializer (seed users)
│   ├── Middleware/                      AuditMiddleware
│   └── Services/                       AuthService
│
└── MarbleCraftOMS.BackgroundServices   ← Background workers (Service Bus consumers)
```

Dependency rule holds: every arrow points inward. Core has no project references; Infrastructure and API reference Core/Application but not each other.

---

## 2. How to Run Locally

**Prerequisites:** .NET 10 SDK, SQL Server LocalDB (ships with VS / VS Build Tools)

```bash
git clone https://github.com/thinkbridge-thinkschool/MarbleCraftOMS.git
cd MarbleCraftOMS
dotnet run --project src/MarbleCraftOMS.Api
```

On first run the app:
1. Applies all pending EF Core migrations to LocalDB automatically
2. Seeds three users (`admin`, `salesagent`, `distributor`)
3. Listens on `https://localhost:7001`

Swagger UI: `https://localhost:7001/swagger`

---

## 3. Happy Path Walkthrough

### Step 1 — Login, get JWT

```bash
curl -sk -X POST https://localhost:7001/api/v1/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-06-24T13:57:01Z",
  "role": "Admin"
}
```

### Step 2 — Hit a protected endpoint with the token

```bash
curl -sk https://localhost:7001/api/v1/me-local \
  -H "Authorization: Bearer <token>"
```

Response — **HTTP 200**:
```json
{
  "message": "Authenticated via local JWT",
  "userId": "1",
  "role": "Admin",
  "distributorId": ""
}
```

No token → **401**. Wrong role for an admin-only endpoint → **403**. The full auth chain is working.

---

## 4. API Endpoints

| Method | Route | Auth policy | Description |
|--------|-------|-------------|-------------|
| GET | `/api/v1/health` | None | Liveness check |
| POST | `/api/v1/login` | None | Get JWT (local dev flow) |
| GET | `/api/v1/me-local` | Local JWT | Verify token claims |
| GET | `/api/v1/products` | Authenticated | Browse catalogue (paginated) |
| POST | `/api/v1/products` | SalesAccess | Add product |
| PUT | `/api/v1/products/{id}` | SalesAccess | Update product |
| DELETE | `/api/v1/products/{id}` | AdminOnly | Delete product |
| GET | `/api/v1/suppliers` | Authenticated | List suppliers |
| POST | `/api/v1/suppliers` | AdminOnly | Add supplier |
| PUT | `/api/v1/suppliers/{id}` | AdminOnly | Update supplier |
| DELETE | `/api/v1/suppliers/{id}` | AdminOnly | Delete supplier |
| GET | `/api/v1/inventory/summary` | Authenticated | Stock summary across all products |
| GET | `/api/v1/inventory/{sku}` | Authenticated | Stock lots for a product |
| POST | `/api/v1/inventory/adjust` | SalesAccess | Commit or release stock |

---

## 5. Seeded Users

| Username | Password | Role |
|----------|----------|------|
| `admin` | `Admin@123` | Admin |
| `salesagent` | `Sales@123` | SalesAgent |
| `distributor` | `Dist@123` | Distributor |

---

## 6. Commit Log (Day 29)

```
4c891bd docs: add demo.http with happy path examples
6ed01be chore: update DI registration, project references, and azure config
ccc83ff feat: add catalogue, supplier, and inventory application services and controllers
a69b446 feat: add auth service, JWT minting, and seed users
5097491 feat: add EF Core configurations, migrations, and repository implementations
5142e66 feat: add core entities, enums, and interfaces (Customer, StockLot, AppUser)
```

---

## 7. Tech Stack

| Concern | Choice |
|---------|--------|
| Runtime | .NET 10 |
| ORM | EF Core 10 (writes + schema) |
| Query layer | Dapper (read-optimised list queries) |
| Database (dev) | SQL Server LocalDB |
| Database (prod) | Azure SQL with Managed Identity (`Authentication=Active Directory Default`) |
| Auth (local) | HS256 JWT via `System.IdentityModel.Tokens.Jwt` |
| Auth (prod) | Azure Entra ID via `Microsoft.Identity.Web` |
| Caching | `IMemoryCache` (5-min TTL on catalogue browse) |
| Rate limiting | ASP.NET Core built-in (`fixed` 100 rpm, `fixed-write` 10 rpm per IP) |
| Observability | OpenTelemetry → Azure Monitor / App Insights |
| Secrets | Azure Key Vault via `DefaultAzureCredential` — zero hardcoded secrets |
