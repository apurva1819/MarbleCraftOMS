# Design Critique — Stock Was a Number. It Should Have Been a Story.

*Top critique from Day 28 design review, and how it changed the domain model.*

---

## The Original Design

```
StockEntry
├── ProductId
├── WarehouseId
├── AvailableQuantity
├── CommittedQuantity
```

Simple, clean — and wrong for this business.

---

## What the Review Found

In a tile importing business, two boxes of "Carrara White 600x600"
from different production batches will look nearly identical in isolation
and visibly different side by side on a floor.

Every tile manufacturer assigns a **lot number** to each production batch
for exactly this reason.

A builder laying 500 square metres of flooring needs every single box
from the same lot number — or they get a call from the architect
when the floor looks patchy.

The original design treated all 300 boxes of Carrara White as interchangeable.
They are not. The design was modelling a generic warehouse system,
not a tile importing business.

---

## What Changed — Three Layers Deep

### Layer 1 — The entity changed

`StockEntry` became `StockLot`:

```
StockLot
├── LotId
├── ProductId
├── WarehouseId
├── LotNumber          ← manufacturer batch ID: "IT-2024-CW-042"
├── AvailableQuantity
├── CommittedQuantity
├── ReceivedAt
├── RowVersion         ← concurrency token — added same session
```

Stock is now tracked per lot, not just per SKU per warehouse.

### Layer 2 — The OrderLine changed

```
OrderLine (before)          OrderLine (after)
├── ProductId               ├── ProductId
├── RequestedQuantity       ├── LotId          ← which batch
├── AllocatedQuantity       ├── WarehouseId    ← from where
├── UnitPrice               ├── RequestedQuantity
                            ├── AllocatedQuantity
                            ├── UnitPrice
```

An order line now records not just what was ordered but exactly which lot
and which warehouse it came from. When dispatch happens, the warehouse
knows exactly which shelf to pull from.

### Layer 3 — The allocation API changed

Before the critique:
```json
POST /api/v1/orders/{id}/allocate
{
  "orderLineId": "...",
  "allocatedQuantity": 50
}
```

After the critique:
```json
POST /api/v1/orders/{id}/allocate
{
  "allocations": [
    {
      "orderLineId": "...",
      "lotId": "...",
      "warehouseId": "...",
      "allocatedQuantity": 50
    }
  ]
}
```

The Sales Manager now explicitly chooses which lot to source from.
That is a business decision — not something the system should guess.

---

## Why This Critique Mattered More Than the Others

Ten design questions were reviewed. Every one found real issues.
The folder structure was wrong. The IDOR vulnerability was serious.
The rate limiter was misconfigured. All of those were fixable
without touching the domain model.

The lot tracking gap was different.

Adding it after Modules 05 and 06 are built means rewriting the stock model,
the allocation logic, the order line schema, and the migration history.
It touches every module.

A security misconfiguration is a bug. A missing lot number is a wrong model
of reality — and wrong models compound. Every feature built on top of them
inherits the wrongness.

**The critique changed how the project is now being built: entity design is
reviewed against the actual business before any module is started, not after.**
