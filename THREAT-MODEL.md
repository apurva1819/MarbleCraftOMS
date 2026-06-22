# Threat Model — MarbleCraftOMS

**Date:** 22 June 2026  
**Method:** STRIDE-lite  
**Scope:** API layer, data tier, messaging, infrastructure  
**Target:** `https://marblecraft-prod-api.livelycoast-9aad040b.eastasia.azurecontainerapps.io`

---

## System Overview

```
[Client] → [Container App: API] → [Azure SQL (private endpoint)]
                                → [Service Bus]
           ↑ Entra ID JWT auth
```

All inbound traffic passes through Entra ID JWT validation before reaching any controller. The SQL server is reachable only via private endpoint inside the VNet — it has no public internet exposure.

---

## STRIDE Analysis

### S — Spoofing

| Asset | Threat | Mitigation | Status |
|---|---|---|---|
| API endpoints | Unauthenticated caller impersonates a user | Azure Entra ID JWT via `Microsoft.Identity.Web`. `[Authorize]` on every controller — no anonymous surface. | ✅ Mitigated |
| Role-protected endpoints | Caller claims a role they don't hold | Roles are issued by Entra ID in the JWT `roles` claim, not sent by the caller. API validates via named policies (`AdminOnly`, `SalesAccess`, etc.). | ✅ Mitigated |

### T — Tampering

| Asset | Threat | Mitigation | Status |
|---|---|---|---|
| Write endpoints (POST/PUT/DELETE) | Caller submits malformed or oversized payloads to corrupt data | `[Required]`, `[MaxLength(200)]`, `[EmailAddress]`, `[Phone]` on all command DTOs. ASP.NET model validation rejects invalid input with 400 before it touches the domain. | ✅ Mitigated |
| Admin-only mutations | Authenticated non-admin caller tampers with supplier/product data | `[Authorize(Policy = "AdminOnly")]` on all write endpoints. 403 returned for insufficient role. | ✅ Mitigated |

### R — Repudiation

| Asset | Threat | Mitigation | Status |
|---|---|---|---|
| All API requests | Caller denies having made a request | `AuditMiddleware` logs `Method`, `Path`, `User` (from JWT identity), `StatusCode`, and UTC timestamp on every request via structured logging → App Insights. | ✅ Mitigated |
| Order mutations | No per-order audit trail | Deferred to Module 06 (Orders). Currently no order entity exists. | ⏳ Deferred |

### I — Information Disclosure

| Asset | Threat | Mitigation | Status |
|---|---|---|---|
| API data | Unauthenticated caller reads supplier/product data | All endpoints require `[Authorize]`. No anonymous GET routes. | ✅ Mitigated |
| Azure SQL | Database reachable from public internet | SQL private endpoint (`marblecraft-prod-sqlserver-pe`) deployed in `marblecraft-prod-vnet`. `publicNetworkAccess: Disabled`. Private DNS zone `privatelink.database.windows.net` routes traffic inside the VNet. | ✅ Mitigated |
| App Insights telemetry | Connection string exposed in config | Connection string sourced from Key Vault at startup via Managed Identity — never hardcoded. | ✅ Mitigated |
| Service Bus | Namespace reachable from public internet | No private endpoint (Premium tier required). Mitigated by Managed Identity auth — no shared-key exposure. | ⏳ Partial |

### D — Denial of Service

| Asset | Threat | Mitigation | Status |
|---|---|---|---|
| Read endpoints | Caller floods GET routes | Fixed-window rate limiter: **100 req/min per IP** → 429. | ✅ Mitigated |
| Write endpoints | Caller floods POST/PUT/DELETE | Stricter fixed-window rate limiter: **10 req/min per IP** → 429. | ✅ Mitigated |
| Audit log | Attacker sends oversized paths to inflate logs | `AuditMiddleware` uses `ReadOnlySpan<char>` to truncate paths to 200 chars before logging — prevents log bloat without heap allocation. | ✅ Mitigated |

### E — Elevation of Privilege

| Asset | Threat | Mitigation | Status |
|---|---|---|---|
| Role boundaries | Authenticated caller accesses endpoints above their role | Five named authorization policies enforced at the controller action level: `AdminOnly`, `WarehouseAccess`, `SalesAccess`, `DistributorAccess`, `InternalOnly`. Role constants defined in `Roles.cs` — no magic strings. | ✅ Mitigated |
| Azure SQL | App uses an over-privileged DB identity | Container App Managed Identity holds minimum required SQL permissions. No `sa` or `db_owner` usage. | ✅ Mitigated |

---

## Infrastructure Security

| Component | Configuration |
|---|---|
| VNet | `marblecraft-prod-vnet` |
| SQL Private Endpoint | `marblecraft-prod-sqlserver-pe` |
| Private DNS Zone | `privatelink.database.windows.net` |
| SQL public access | `Disabled` (enforced in Bicep when `privateEndpointSubnetId` is set) |
| Secret management | Azure Key Vault — Managed Identity access, no connection strings in config files |
| Container registry | AcrPull role assigned to Container App Managed Identity — no admin credentials |

---

## OWASP ZAP Baseline — 22 June 2026

**Target:** `https://marblecraft-prod-api.livelycoast-9aad040b.eastasia.azurecontainerapps.io`

| Result | Count |
|---|---|
| FAIL-NEW (blocking) | 0 |
| WARN-NEW | 2 |
| PASS | 65 |

### Warnings raised

| Alert | ID | Resolution |
|---|---|---|
| Strict-Transport-Security header not set | 10035 | Deferred — HSTS is enforced at the Azure Front Door / Container App ingress layer in production. Adding it at the API level would duplicate it. |
| Storable and Cacheable Content | 10049 | Deferred — API returns JSON data that is correctly not marked no-store. No sensitive data is returned without auth. Cache-Control headers will be tightened in a follow-up. |

---

## Deferred Items

| Item | Reason |
|---|---|
| Service Bus private endpoint | Requires Azure Service Bus Premium tier upgrade |
| Per-order audit log | Orders entity not yet implemented (Module 06) |
| Azure Defender for SQL | Production hardening — cost vs. risk accepted for now |
| Cache-Control headers | Low risk given all endpoints are auth-gated |
