# MarbleCraft OMS — Day 22 Design Document

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

## Two Users

| User | Role |
|------|------|
| **Sales Manager** | MarbleCraft staff — manages products, reviews orders, allocates stock, monitors inventory |
| **Distributor** | External buyer — browses available tiles, places bulk orders, tracks order status |


---

## Bounded Contexts

### 1. Identity
Handles who can log in and what they are allowed to do.
- Sales Manager logs in with a staff account
- Distributor logs in with a distributor account
- JWT issued on login, role embedded in claims
- No guest access — everything requires login

### 2. Catalogue
Manages the tile product master.
- Sales Manager adds and updates products (SKU, name, collection, size, finish, material, origin)
- Distributors browse the catalogue and see available stock per SKU
- Read-heavy — Dapper on the browse endpoint

### 3. Inventory
Tracks stock across warehouses.
- Each SKU has a stock count per warehouse
- Stock has two states: **Available** and **Committed**
- When an order is placed, stock moves from Available to Committed
- When an order is confirmed and dispatched, Committed stock is consumed
- Sales Manager can see total available, total committed, and total on-hand per SKU

### 4. Orders
The core of the system.
- Distributor places an order (one or more SKUs with quantities)
- Sales Manager reviews, allocates stock, confirms
- Order moves through a status lifecycle
- Sales Manager can reject an order if stock is insufficient

### 5. Notifications
Async, background-driven alerts.
- Fires when stock falls below a defined threshold
- Fires when an order status changes
- Consumed by Sales Manager and Distributor respectively

---

## Core Aggregate — DistributorOrder

`DistributorOrder` is the central object the entire system revolves around.

```
DistributorOrder
├── OrderId
├── Distributor (who placed it)
├── OrderLines[]
│     ├── SKU
│     ├── RequestedQuantity
│     ├── AllocatedQuantity
│     └── UnitPrice
├── Status
│     Pending → Allocated → Confirmed → Dispatched → Delivered
├── PlacedAt
├── ConfirmedAt
└── Notes
```

**Business rules inside the aggregate:**
- An order cannot be confirmed unless every line has allocated quantity > 0
- Allocated quantity cannot exceed available stock at time of allocation
- Once Dispatched, the order cannot be modified
- Cancellation is only allowed in Pending or Allocated status

---

## Async Flows

### Flow 1 — Low Stock Alert
```
Sales Manager confirms an order
        ↓
Stock for each allocated SKU is consumed
        ↓
System checks: is remaining stock below threshold?
        ↓ (if yes)
LowStockEvent published to Azure Service Bus
        ↓
Notification consumer picks it up
        ↓
Sales Manager receives alert:
"Carrara White 600x600 — only 120 boxes remaining. Consider next import."
```

### Flow 2 — Order Status Change Notification
```
Sales Manager updates order status
(Allocated → Confirmed, or Confirmed → Dispatched)
        ↓
OrderStatusChangedEvent published to Azure Service Bus
        ↓
Notification consumer picks it up
        ↓
Distributor receives alert:
"Your order #ORD-2024-0042 has been dispatched."
```

---

## Solution Structure

```
MarbleCraftOMS/
├── src/
│   ├── MarbleCraftOMS.Api/                  # ASP.NET Core 10 Web API
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── CatalogueController.cs
│   │   │   ├── InventoryController.cs
│   │   │   └── OrdersController.cs
│   │   ├── Middleware/
│   │   ├── Program.cs
│   │   └── MarbleCraftOMS.Api.csproj
│   │
│   ├── MarbleCraftOMS.Core/                 # Domain — no dependencies
│   │   ├── Entities/
│   │   │   ├── DistributorOrder.cs          # Core aggregate
│   │   │   ├── OrderLine.cs
│   │   │   ├── Product.cs
│   │   │   └── StockEntry.cs
│   │   ├── Events/
│   │   │   ├── LowStockEvent.cs
│   │   │   └── OrderStatusChangedEvent.cs
│   │   ├── Interfaces/
│   │   │   ├── IOrderRepository.cs
│   │   │   ├── IInventoryRepository.cs
│   │   │   └── ICatalogueRepository.cs
│   │   └── MarbleCraftOMS.Core.csproj
│   │
│   ├── MarbleCraftOMS.Application/          # Use cases / service layer
│   │   ├── Orders/
│   │   │   ├── PlaceOrderCommand.cs
│   │   │   ├── AllocateStockCommand.cs
│   │   │   └── ConfirmOrderCommand.cs
│   │   ├── Inventory/
│   │   │   └── CheckStockQuery.cs
│   │   ├── Catalogue/
│   │   │   └── GetProductsQuery.cs
│   │   └── MarbleCraftOMS.Application.csproj
│   │
│   ├── MarbleCraftOMS.Infrastructure/       # EF Core, Dapper, Service Bus
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Migrations/
│   │   │   ├── Repositories/
│   │   │   │   ├── OrderRepository.cs
│   │   │   │   ├── InventoryRepository.cs
│   │   │   │   └── CatalogueRepository.cs
│   │   │   └── DapperQueries/
│   │   │       └── StockSummaryQuery.cs
│   │   ├── Messaging/
│   │   │   └── ServiceBusPublisher.cs
│   │   └── MarbleCraftOMS.Infrastructure.csproj
│   │
│   └── MarbleCraftOMS.BackgroundServices/   # Hosted services
│       ├── LowStockMonitor.cs
│       ├── NotificationConsumer.cs
│       └── MarbleCraftOMS.BackgroundServices.csproj
│
├── tests/
│   ├── MarbleCraftOMS.UnitTests/
│   │   └── MarbleCraftOMS.UnitTests.csproj
│   └── MarbleCraftOMS.IntegrationTests/
│       └── MarbleCraftOMS.IntegrationTests.csproj
│
├── .github/
│   └── workflows/
│       └── ci.yml
│
└── MarbleCraftOMS.sln
```

---

## Why Modular Monolith

Each bounded context (Identity, Catalogue, Inventory, Orders, Notifications) is a
separate folder with its own interfaces and models. They communicate through the
Application layer — not through direct database joins across contexts.

This means the system can be split into separate services later if MarbleCraft grows —
without rewriting the domain logic.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend API | ASP.NET Core 10 |
| ORM (writes) | EF Core 10 |
| Raw reads | Dapper |
| Database | SQL Server |
| Async messaging | Azure Service Bus |
| Background jobs | BackgroundService + Channel |
| Frontend | Angular 21 |
| Auth | JWT (Sales Manager + Distributor roles) |
| Deploy | Azure Container Apps + Static Web Apps |
| CI/CD | GitHub Actions |
