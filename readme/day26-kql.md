# Day 26 — KQL Queries

## Query 1 — p50 / p99 latency by endpoint

```kql
requests
| summarize p50=percentile(duration, 50),
            p99=percentile(duration, 99)
  by name
| order by p99 desc
```

## Query 2 — Dependency call breakdown

```kql
dependencies
| summarize count(), avg(duration)
  by type, target
| order by avg_duration desc
```

## Query 3 — Error rate alert (5-minute buckets)

```kql
requests
| summarize total=count(),
            failed=countif(success == false)
  by bin(timestamp, 5m)
| extend errorRate = (failed * 100.0) / total
| where errorRate > 5
```
