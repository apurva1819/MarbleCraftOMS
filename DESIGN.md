# MarbleCraft OMS — Design Document

## What Are We Building

MarbleCraft Imports Pvt Ltd imports premium ceramic, vitrified, and porcelain tiles
from Italy and Spain. They sell to distributors across India who resell to builders,
architects, and retail showrooms.

**The core problem:**
No single person in the company can answer this question in real time:
> "How much stock is available right now, how much is already committed to open orders,
> and do we need to import more?"

**MarbleCraft OMS** solves exactly that — a Distributor Order and Stock Allocation
platform that lets distributors place orders and lets the sales team manage stock,
confirm allocations, and get alerted when inventory runs low.

---

## System Roles

| Role | Description |
|------|-------------|
| **Admin** | Full system access, user management, configuration |
| **SalesAgent** | Manages orders: confirm, dispatch; manages products and suppliers |
| **WarehouseStaff** | Adjusts lot stock, monitors inventory quantities |
| **Distributor** | Browses catalogue, places and tracks own orders |

---

## Bounded Contexts

### 1. Identity
Handles who can log in and what they are allowed to do.
- Local JWT (HMAC-SHA256) for dev/demo; Azure Entra ID for production
- Role embedded in JWT claims: Admin, SalesAgent, WarehouseStaff, Distributor
- Policy scheme routes to correct validator based on token issuer
- No guest access — everything requires login

### 2. Catalogue
Manages the tile product master.
- SalesAgent adds and updates products (SKU, name, collection, size, finish, material, origin)
- Distributors browse the catalogue and see available stock per SKU
- Read-heavy — Dapper with raw SQL on the browse endpoint; IMemoryCache 5-min TTL

### 3. Inventory
Tracks stock at lot level — not just SKU level.
- Each SKU has one or more StockLots (quarry batch lots with a LotNumber)
- Stock has two states per lot: **Available** (OnHand − Committed) and **Committed**
- When an order is placed, stock commits immediately from the selected lot
- WarehouseStaff adjusts lot stock; every adjustment is audit-logged
- SalesAgent and Admin can view stock summary and lot detail

### 4. Orders
The core of the system.
- Distributor places an order (one or more lines, each tied to a specific StockLot)
- Stock is committed atomically at placement — no race conditions, no overselling
- Order moves through a strict state machine enforced by domain entity guard clauses
- SalesAgent confirms and dispatches; cancellation releases committed stock immediately

### 5. Notifications
Async, background-driven alerts.
- Fires when stock falls below a defined threshold (LowStockMonitor, every 5 minutes)
- Fires when an order status changes (OrderStatusChangedEvent)
- Consumed by a background NotificationConsumer writing to the Notifications table
- Distributor and Admin poll GET /notifications; mark read via PATCH

### 6. Suppliers
Manages the supplier master.
- Full CRUD — AdminOnly on writes
- Suppliers are linked to Products and StockLots

### 7. Customers
Manages distributor accounts.
- Full CRUD — AdminOnly on writes
- Customers are linked to DistributorOrders and AppUsers (via DistributorId)

### 8. Users
Admin-only user management.
- Create, update, delete system users
- Passwords hashed with BCrypt (work factor 12); PasswordHash never exposed via API
- Role assignment validated against the four known roles

---

## Core Aggregate — DistributorOrder

`DistributorOrder` is the central object the entire system revolves around.

```
DistributorOrder
├── OrderId
├── CustomerId (Distributor who placed it)
├── OrderLines[]
│     ├── ProductId
│     ├── StockLotId          ← lot-level, not just SKU-level
│     ├── Quantity
│     └── UnitPrice
├── Status
│     Pending → Confirmed → Dispatched
│          ↓
│       Cancelled (from Pending or Confirmed only)
├── OrderDate
├── CreatedAt
└── Notes
```

**Business rules inside the aggregate (guard clauses on the entity):**
- Stock is committed at placement — not at confirmation
- Cannot confirm an order that is not in Pending status
- Cannot dispatch an order that is not in Confirmed status
- Cannot cancel a Dispatched order
- Cancellation releases committed stock back to available immediately
- Once Dispatched, the order is locked forever

---

## Async Flows

### Flow 1 — Low Stock Alert
```
LowStockMonitor runs every 5 minutes
        ↓
Queries StockLots where (OnHand − Committed) ≤ 50
        ↓
Publishes LowStockEvent to in-memory Channel<IDomainEvent>
        ↓
NotificationConsumer picks it up
        ↓
Writes Notification row to DB
        ↓
Admin/SalesAgent sees it on next GET /notifications poll
```

### Flow 2 — Order Status Change Notification
```
SalesAgent confirms or dispatches an order
        ↓
OrderStatusChangedEvent published to in-memory Channel<IDomainEvent>
        ↓
NotificationConsumer picks it up
        ↓
Writes Notification row (CustomerId set = distributor-specific)
        ↓
Distributor sees it on next GET /notifications poll
```

> **Note on messaging:** `IEventBus` is backed by `System.Threading.Channels` for
> single-instance deployments. Azure Service Bus is provisioned in Bicep for
> production scale-out. Swapping Channel → Service Bus requires only a one-line
> change in Program.cs — no domain or consumer code changes.

---

## Solution Structure

```
MarbleCraftOMS/
├── src/
│   ├── MarbleCraftOMS.Api/                  # ASP.NET Core 10 Web API
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── CustomersController.cs
│   │   │   ├── InventoryController.cs
│   │   │   ├── NotificationsController.cs
│   │   │   ├── OrdersController.cs
│   │   │   ├── ProductsController.cs
│   │   │   ├── SuppliersController.cs
│   │   │   └── UsersController.cs
│   │   ├── Middleware/
│   │   │   ├── AuditMiddleware.cs           # logs every request post-execution
│   │   │   └── GlobalExceptionHandler.cs   # maps domain exceptions → HTTP codes
│   │   ├── Services/
│   │   │   ├── AuthService.cs
│   │   │   └── UserService.cs
│   │   ├── Data/
│   │   │   └── DbInitializer.cs             # seeds users on first run
│   │   └── Program.cs
│   │
│   ├── MarbleCraftOMS.Core/                 # Domain — no external dependencies
│   │   ├── Entities/
│   │   │   ├── AppUser.cs
│   │   │   ├── Customer.cs
│   │   │   ├── DistributorOrder.cs          # core aggregate
│   │   │   ├── Notification.cs
│   │   │   ├── OrderLine.cs
│   │   │   ├── Product.cs
│   │   │   ├── StockLot.cs                  # lot-level inventory tracking
│   │   │   └── Supplier.cs
│   │   ├── Enums/
│   │   │   └── OrderStatus.cs
│   │   ├── Events/
│   │   │   ├── LowStockEvent.cs
│   │   │   └── OrderStatusChangedEvent.cs
│   │   ├── Constants/
│   │   │   └── Roles.cs
│   │   └── Interfaces/
│   │       ├── ICustomerRepository.cs
│   │       ├── IEventBus.cs
│   │       ├── IInventoryRepository.cs
│   │       ├── INotificationRepository.cs
│   │       ├── IOrderRepository.cs
│   │       ├── IProductRepository.cs
│   │       ├── ISupplierRepository.cs
│   │       └── IUserRepository.cs
│   │
│   ├── MarbleCraftOMS.Application/          # Use cases / service layer
│   │   ├── Auth/
│   │   ├── Catalogue/                       # ProductService, browse query
│   │   ├── Customers/                       # CustomerService
│   │   ├── Inventory/                       # InventoryService, stock queries
│   │   ├── Notifications/                   # NotificationService
│   │   ├── Orders/                          # OrderService
│   │   ├── Supplier/                        # SupplierService
│   │   └── Users/                           # IUserService, DTOs, commands
│   │
│   ├── MarbleCraftOMS.Infrastructure/       # EF Core, Dapper, messaging
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/              # Fluent API entity configs
│   │   │   ├── Migrations/
│   │   │   ├── Repositories/
│   │   │   │   ├── CustomerRepository.cs
│   │   │   │   ├── InventoryRepository.cs
│   │   │   │   ├── NotificationRepository.cs
│   │   │   │   ├── OrderRepository.cs
│   │   │   │   ├── ProductRepository.cs
│   │   │   │   ├── SupplierRepository.cs
│   │   │   │   └── UserRepository.cs
│   │   │   └── DapperQueries/
│   │   │       ├── ProductBrowseQuery.cs    # paginated catalogue with stock join
│   │   │       └── StockSummaryQuery.cs     # aggregate stock per product
│   │   ├── Messaging/
│   │   │   └── InMemoryEventBus.cs          # Channel<IDomainEvent>
│   │   └── Services/
│   │       └── MemoryCacheAdapter.cs
│   │
│   └── MarbleCraftOMS.BackgroundServices/  # Hosted services
│       ├── LowStockMonitor.cs               # timer-based, every 5 min
│       └── NotificationConsumer.cs          # Channel consumer → DB writer
│
├── tests/
│   ├── MarbleCraftOMS.UnitTests/
│   └── MarbleCraftOMS.IntegrationTests/
│
├── marble-craft-oms/                        # Angular 21 frontend
│
├── infra/                                   # Bicep IaC
│   ├── main.bicep
│   └── modules/
│       ├── api.bicep
│       ├── network.bicep
│       ├── sql.bicep
│       └── servicebus.bicep
│
├── k6/                                      # load tests
├── .github/workflows/ci.yml
└── MarbleCraftOMS.sln
```

---

## Why Modular Monolith

Each bounded context (Identity, Catalogue, Inventory, Orders, Notifications,
Suppliers, Customers, Users) is a separate folder with its own interfaces and models.
They communicate through the Application layer — not through direct database joins
across contexts.

This means the system can be split into separate services later if MarbleCraft grows —
without rewriting the domain logic.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend API | ASP.NET Core 10 |
| ORM (writes) | EF Core 10 |
| Raw reads | Dapper |
| Database | SQL Server (Azure SQL in prod, LocalDB in dev) |
| Async messaging | In-memory `Channel<IDomainEvent>` (Azure Service Bus provisioned in Bicep for scale-out) |
| Background jobs | `BackgroundService` + `Channel<T>` |
| Frontend | Angular 21 |
| Auth | JWT — Local (HMAC-SHA256) for dev; Azure Entra ID for prod |
| Secrets | Azure Key Vault via Managed Identity |
| Deploy | Azure Container Apps + Bicep IaC (azd) |
| CI/CD | GitHub Actions |
