# MarbleCraft OMS — Day-by-Day Build Plan

> **Day 22 = Day 1 of the capstone.**
> The first four weeks covered fundamentals on the QuotesAPI.
> From Day 22 the same patterns are applied to a real business domain.

---

## Day 22 — Solution scaffolding
*Get the skeleton running before writing a single line of business logic.*

Five projects created and wired. AppDbContext registered. GitHub repo with
protected main branch. A Hello World controller proves the DI and routing work.

**Done when** `dotnet run` shows Swagger UI and the first migration
creates an empty database.

---

## Day 23 — Core domain entities
*The heart of the system — no HTTP, no database yet. Pure domain logic.*

`DistributorOrder` aggregate with business rules enforced inside the entity,
not the service layer. `Product` and `StockLot` entities. EF Core FluentAPI
config. First migration applied.

**Done when** unit tests for every domain guard clause pass.
The entity rejects invalid state changes before the database is ever involved.

---

## Day 24 — Identity: lock the doors before opening them
*Nothing works until the system knows exactly who is calling.*

Azure Entra ID as the identity provider. Four roles: SalesManager, Distributor,
WarehouseStaff, Admin. Authorization policies defined once in `Program.cs`,
applied everywhere. Every controller is `[Authorize]` by default.

**Done when** a Distributor token gets `403` on a manager route.
No token gets `401`. Stack traces never appear in any error response.

---

## Day 25 — Catalogue API: products that know their origins
*Distributors browse the catalogue constantly — read-heavy means Dapper.*

Supplier entity as the source of product origin. Paginated product list with
live stock count via a Dapper JOIN query. SalesManager can add and update
products via EF Core writes.

**Done when** `GET /catalogue/products` returns a paginated list with
available stock per SKU. A product knows which supplier it came from.

---

## Day 26 — Inventory: stock that knows its own story
*Available vs Committed is the core insight of this entire system.*

`StockLot` entity with `Reserve()`, `Consume()`, and `Release()` as the only
ways to change stock state. `RowVersion` concurrency token on every lot row —
overselling is architecturally impossible.

**Done when** attempting to reserve more stock than available throws a named
business exception. Cancelling an order returns committed stock to available.

---

## Day 27 — Orders: place an order — one atomic action
*Distributor submits — stock moves, a record is created, a reference number
comes back. One transaction. No partial state possible.*

`PlaceOrderCommand` wraps order creation and all stock reservations in a single
`SaveChangesAsync`. `OutboxMessages` table written in the same transaction.
`OrderStatusHistory` records every transition permanently.

**Done when** `POST /api/orders` creates the order, commits stock, and writes
an outbox row — all atomically. Stock shows as Committed immediately.

---

## Day 28 — Orders: allocate, confirm, dispatch
*The Sales Manager workflow. Every transition has a guard.
An invalid state change returns 409 — never silent.*

Sub-resource endpoints for each transition: `/allocate`, `/confirm`,
`/dispatch`, `/cancel`, `/reject`. State machine methods live inside
the `DistributorOrder` aggregate, not the controller. IDOR check on every
endpoint — a distributor cannot touch another distributor's order.

**Done when** the full order lifecycle works end-to-end. Every invalid
transition returns `409`. Confirming an order writes a row to `OutboxMessages`.

---

## Day 29 — Async flows: events that never get lost
*The order aggregate has no idea notifications exist.
The consumer has no idea about orders. This is the design paying off.*

`OutboxProcessor` polls `OutboxMessages` every 30 seconds and publishes to
Azure Service Bus. `NotificationConsumer` checks idempotency before every
message. Low-stock alert fires when confirmed order drains a lot below threshold.

**Done when** Service Bus is taken offline, an order is confirmed, Service Bus
is restored — the notification arrives. No messages lost.

---

## Day 30 — Angular: auth + catalogue
*Frontend is presentation. The backend is the truth.*

Angular app with MSAL for Entra ID auth. JWT interceptor attaches the
Authorization header to every HTTP call. AuthGuard redirects unauthenticated
users. Product catalogue grid with SKU, name, finish, size, and available-stock
badge.

**Done when** a Distributor logs in, sees the product catalogue, and the token
is attached to all outgoing requests automatically.

---

## Day 31 — Orders UI + ship it
*Last day. CI must be green. Demo runs without narrating around bugs.*

Order placement form, Manager order queue with Allocate and Confirm buttons.
GitHub Actions CI pipeline: restore → build → test → docker build.
End-to-end demo covering every role.

**Done when** a Distributor places an order in production, a Sales Manager
confirms it, the Distributor receives a notification, and the entire flow is
visible in App Insights.

---

## The through-line

```
Day 22  →  Can the skeleton run?
Day 23  →  Do the business rules protect themselves?
Day 24  →  Who is allowed in?
Day 25  →  What can they see?
Day 26  →  What stock exists and how does it move?
Day 27  →  How does an order get placed?
Day 28  →  How does an order get fulfilled?
Day 29  →  What happens when infrastructure fails?
Day 30  →  Can a distributor use this from a browser?
Day 31  →  Is it production-ready?
```

Each day answers one question.
Each answer depends on all the answers before it.
