# ADR-003: Transactional Outbox Pattern


| **Status** | Accepted |
| **Decision made** | Day 26 — before Orders module was built |
| **Affects** | Every event published in the system |

---

## Context

When a Sales Manager confirms an order, two things must happen:

1. Order status saved to **Azure SQL** — `Status = Confirmed`
2. `OrderStatusChangedEvent` published to **Azure Service Bus** — so the Distributor gets notified

These are two separate external systems. There is no single transaction that covers both.

This creates a real failure gap:

```
If the app crashes between the two writes:

  Order saved as Confirmed  ✅
  Event never published     ❌
  Distributor never notified — silently, with no trace of the failure
```

In development this never happens. In production it happens every week — network
hiccups, transient timeouts, container restarts. The naive implementation looks
correct until 2am when a distributor calls to say their order was confirmed three
days ago and they heard nothing.

---

## Decision

**Write the event into an `OutboxMessages` table inside the same database
transaction as the state change. A background job publishes it to Service Bus separately.**

```
Step 1 — ConfirmOrderCommand (one DB transaction):
┌──────────────────────────────────────────────┐
│  UPDATE Orders SET Status = 'Confirmed'      │
│  INSERT INTO OutboxMessages (event payload)  │
│  COMMIT                                      │
└──────────────────────────────────────────────┘
  Both rows saved or neither is. Atomic.

Step 2 — API responds to Sales Manager immediately.
  The Sales Manager is never waiting for Service Bus.

Step 3 — OutboxProcessor (background job, every 30 seconds):
  Reads unprocessed OutboxMessages
  → Publishes to Service Bus
  → Marks ProcessedAt = now

Step 4 — NotificationConsumer receives the event
  → Sends notification to Distributor
  → Acknowledges to Service Bus only after success
```

The command handler never touches Service Bus. It only knows about the database.

```csharp
// ConfirmOrderCommand handler — no Service Bus reference anywhere
public async Task Handle(ConfirmOrderCommand cmd)
{
    var order = await _orderRepository.GetByIdAsync(cmd.OrderId);
    order.Confirm();  // state machine method on the aggregate

    _context.OutboxMessages.Add(new OutboxMessage
    {
        Id        = Guid.NewGuid(),
        EventType = nameof(OrderStatusChangedEvent),
        Payload   = JsonSerializer.Serialize(new OrderStatusChangedEvent(order)),
        CreatedAt = DateTime.UtcNow
    });

    await _context.SaveChangesAsync(); // order + outbox row — one transaction
}
```

```csharp
// OutboxProcessor — the only thing that touches Service Bus
protected override async Task ExecuteAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        var pending = await _context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        foreach (var message in pending)
        {
            await _publisher.PublishAsync(message.EventType, message.Payload, ct);
            message.ProcessedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
    }
}
```

---

## Alternatives Rejected

**Option A — Publish directly inside the command handler**

```csharp
await _orderRepository.SaveAsync(order);         // DB
await _serviceBusPublisher.PublishAsync(event);  // Service Bus
```

Rejected. These two lines are not atomic. Any failure between them — a network
timeout, a container restart, a Service Bus throttle — produces a confirmed order
with no notification. Silently. Permanently. No retry, no record, no alert.

**Option B — Publish before saving**

```csharp
await _serviceBusPublisher.PublishAsync(event);  // Service Bus first
await _orderRepository.SaveAsync(order);         // DB second
```

Rejected. The notification fires before the data exists. The consumer wakes up,
queries the order, and gets a 404. Worse than Option A — a ghost notification
for an operation that may still fail.

**Option C — Change Data Capture (CDC)**

Detect row changes in SQL Server and feed them to Service Bus via Azure
infrastructure. Rejected. CDC requires SQL Server Agent — not available on Azure
SQL Standard tier without additional cost. More critically, it couples event
publishing to database infrastructure. A column rename on the Orders table
silently breaks the event pipeline with no compile-time warning.

---

## Consequences

**Gained:**

- Order confirmation and event write are in one transaction — either both land in
  the database or neither does. No state where an order is confirmed but the
  notification was never queued.
- `OutboxMessages` table is a permanent, queryable record of every event.
  When a distributor says "I never got the notification," the answer is one SQL
  query away — not a guess.
- Events survive Service Bus outages. If Service Bus is unavailable for two hours,
  messages queue in the outbox and drain automatically when it recovers.

**Costs:**

- Notifications are delayed by up to 30 seconds (the polling interval). Reducible
  to 5 seconds if real-time notification becomes a requirement.
- Service Bus guarantees at-least-once delivery, not exactly-once. If the
  OutboxProcessor crashes after publishing but before writing `ProcessedAt`, the
  same message is published again on the next poll. Every consumer must check a
  `ProcessedMessageIds` table before acting.
- `OutboxMessages` grows indefinitely if processed rows are never removed. A
  cleanup job must run weekly to delete records older than 30 days.
- The OutboxProcessor runs as a single BackgroundService. If the API scales to
  multiple replicas, multiple processors race to publish the same rows. A
  `SKIP LOCKED` query hint on the outbox poll resolves this before scaling.

---

## Why This Is the Most Important Decision in the System

Every other decision in this project affects performance or maintainability.
This one affects correctness.

A Distributor who misses a dispatch notification may hold up a construction site.
That is a real business consequence, not a technical metric.
The Outbox Pattern makes the wrong outcome architecturally impossible —
not just unlikely.
