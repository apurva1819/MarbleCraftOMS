# Day 32 — Ship · Demo · Postmortem

> *"From chaos to clarity — every order, tracked."*

---

## What Was Shipped

MarbleCraftOMS is a full-stack, role-based B2B Order Management System for a marble tile importing business. Built over 32 days as part of the ThinkBridge × ThinkSchool Full-Stack Development Internship.

| Layer | What shipped |
|---|---|
| Backend | ASP.NET Core 10 REST API — 8 modules, 32+ endpoints, Clean Architecture |
| Database | SQL Server + EF Core migrations + Dapper read queries |
| Auth | Dual-scheme JWT (local dev + Azure Entra ID prod), 4 roles |
| Infra | Azure Container Apps, Bicep IaC, Key Vault, VNet with private endpoints |
| Frontend | Angular 21 — Distributor + SalesAgent + Admin flows |
| Testing | Unit + Integration + E2E + k6 load tests + OWASP ZAP security scan |
| CI/CD | GitHub Actions — restore → build → test → green gate on every push |

---

## Live Demo

The screen recording walks through the full order lifecycle:

1. **Distributor** logs in → browses live catalogue with lot-level stock
2. **Distributor** places an order → stock commits atomically
3. **SalesAgent** logs in → sees pending order in queue → confirms it
4. **SalesAgent** dispatches → order locked, notifications fired
5. **Admin** manages users and customers through the API

---

## Postmortem

### What I'd do differently

**Start with the full module list, not just the happy path.**
I built the order flow first — which was correct — but left Customers and Users as "entity only, no API" for too long. They weren't discovered until a spec-vs-reality audit near the end. If I had mapped all 8 modules to controllers on Day 1 (even empty ones), the gap would have been visible from the start instead of a late fix.

**Design the role matrix before writing a single controller.**
The WarehouseStaff role was defined in constants, had its own authorization policy, and was described in the presentation — but `InventoryController` was accidentally wired to `SalesAccess` instead of `WarehouseAccess`. A WarehouseStaff token would have received 403 on the one endpoint it was supposed to own. A role-to-endpoint matrix written on Day 1 would have caught this immediately.

**Pick a consistent HTTP status for `ArgumentException` early.**
Some controllers mapped it to 400, others to 422. Small inconsistency, but it shows up in tests and client error handling. Deciding the mapping once in a global handler (which we eventually added) was the right move — just should have happened sooner.

---

### What the hardest bug taught me

The hardest issue wasn't a runtime crash — it was the **silent WarehouseStaff 403**. The app compiled, all tests passed, CI was green. But a real WarehouseStaff user logging in would have been silently locked out of inventory adjustments with no useful error.

What it taught me: **correctness isn't just "does it build and pass tests." It's "does the system behave exactly as described for every actor."** I now treat role-based access as something that needs an explicit test per role per endpoint — not just a trust that the attribute is right.

---

### The one thing I'm proudest of

**`StockLot.CommitStock()` — the domain invariant that makes the whole system work.**

```csharp
public void CommitStock(int quantity)
{
    if (quantity <= 0)
        throw new ArgumentException("Quantity must be positive.", nameof(quantity));
    if (quantity > QuantityAvailable)
        throw new InvalidOperationException(
            $"Only {QuantityAvailable} units available in lot {LotNumber}.");
    QuantityCommitted += quantity;
}
```

Six lines. But this is the line that prevents overselling. Two distributors cannot both commit the last 50 boxes of Carrara White because EF Core's transaction + this guard clause make it physically impossible to reach an invalid state. The invariant lives on the entity — not in a service, not in a controller, not in a database trigger. It cannot be bypassed by any code path that skips the service layer.

That's the thing I'd point to if someone asked: *"Did you understand what you built?"*

---

## Stats

| Metric | Value |
|---|---|
| Days | 32 |
| C# projects | 7 (5 source + 2 test) |
| API endpoints | 32+ |
| Domain entities | 8 |
| Tests | 21+ (unit + integration) |
| k6 p95 read latency | ~9ms |
| k6 p95 write latency | ~6ms |
| OWASP ZAP FAIL-NEW findings | 0 |
| CI pipeline status | Green |

---

*Apurva Patil — ThinkBridge × ThinkSchool Internship 2025*
