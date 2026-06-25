import http from "k6/http";
import { check, group, sleep } from "k6";
import { Rate, Trend, Counter } from "k6/metrics";

// ── Custom metrics ──────────────────────────────────────────────────────────
const errorRate    = new Rate("errors");
const loginLatency = new Trend("login_latency", true);
const orderLatency = new Trend("order_latency", true);
const readLatency  = new Trend("read_latency",  true);

// ── Thresholds ───────────────────────────────────────────────────────────────
// p95 read < 200 ms, p95 write < 500 ms, error rate < 1%
export const options = {
  stages: [
    { duration: "15s", target: 10 },   // ramp up
    { duration: "30s", target: 20 },   // peak load
    { duration: "15s", target: 5  },   // ramp down
    { duration: "10s", target: 0  },   // drain
  ],
  thresholds: {
    http_req_failed:  ["rate<0.01"],           // < 1% HTTP errors
    http_req_duration: ["p(95)<500"],          // 95th pct overall < 500 ms
    read_latency:      ["p(95)<200"],          // reads faster
    order_latency:     ["p(95)<600"],          // writes allowed a bit more
    errors:            ["rate<0.01"],
  },
};

const BASE = "http://localhost:8080/api/v1";

// IDs pre-seeded via docker exec before the run (passed with --env PRODUCT_ID=X STOCK_LOT_ID=Y)
const PRODUCT_ID   = parseInt(__ENV.PRODUCT_ID   || "3");
const STOCK_LOT_ID = parseInt(__ENV.STOCK_LOT_ID || "4");

// ── Setup: mint tokens once, share via __ENV-style return value ──────────────
export function setup() {
  const adminToken = login("admin",      "Admin@123");
  const salesToken = login("salesagent", "Sales@123");
  const distToken  = login("distributor","Dist@123");

  return {
    adminToken,
    salesToken,
    distToken,
    productId:   PRODUCT_ID,
    stockLotId:  STOCK_LOT_ID,
  };
}

// ── Default function: executed by every VU on every iteration ─────────────
export default function (data) {
  const { adminToken, salesToken, distToken, productId, stockLotId } = data;

  // Each VU randomly picks a scenario weighted toward reads (realistic mix)
  const r = Math.random();

  if (r < 0.35) {
    scenarioReadProducts(adminToken);
  } else if (r < 0.55) {
    scenarioReadSuppliers(adminToken);
  } else if (r < 0.70) {
    scenarioReadNotifications(salesToken);
  } else if (r < 0.85) {
    scenarioPlaceAndCancelOrder(salesToken, productId, stockLotId);
  } else {
    scenarioPlaceConfirmDispatch(salesToken, productId, stockLotId);
  }

  sleep(0.5);
}

// ── Scenarios ────────────────────────────────────────────────────────────────

function scenarioReadProducts(token) {
  group("GET /products", () => {
    const res = authedGet(token, `${BASE}/products?page=1&pageSize=10`);
    readLatency.add(res.timings.duration);
    const ok = check(res, {
      "products 200": (r) => r.status === 200,
    });
    errorRate.add(!ok);
  });
}

function scenarioReadSuppliers(token) {
  group("GET /suppliers", () => {
    const res = authedGet(token, `${BASE}/suppliers`);
    readLatency.add(res.timings.duration);
    const ok = check(res, {
      "suppliers 200": (r) => r.status === 200,
    });
    errorRate.add(!ok);
  });
}

function scenarioReadNotifications(token) {
  group("GET /notifications", () => {
    const res = authedGet(token, `${BASE}/notifications?count=10`);
    readLatency.add(res.timings.duration);
    const ok = check(res, {
      "notifications 200": (r) => r.status === 200,
    });
    errorRate.add(!ok);
  });
}

function scenarioPlaceAndCancelOrder(token, productId, stockLotId) {
  group("POST /orders then DELETE", () => {
    const placeRes = placeOrder(token, productId, stockLotId, 1);
    orderLatency.add(placeRes.timings.duration);

    const placed = check(placeRes, {
      "place order 201": (r) => r.status === 201,
    });
    errorRate.add(!placed);

    if (placed) {
      const body    = JSON.parse(placeRes.body);
      const orderId = body.orderId;

      const cancelRes = authedDelete(token, `${BASE}/orders/${orderId}`);
      const cancelled = check(cancelRes, {
        "cancel pending 204": (r) => r.status === 204,
      });
      errorRate.add(!cancelled);
    }
  });
}

function scenarioPlaceConfirmDispatch(token, productId, stockLotId) {
  group("Place → Confirm → Dispatch", () => {
    const placeRes = placeOrder(token, productId, stockLotId, 1);
    orderLatency.add(placeRes.timings.duration);

    const placed = check(placeRes, {
      "place 201": (r) => r.status === 201,
    });
    errorRate.add(!placed);

    if (!placed) return;

    const orderId = JSON.parse(placeRes.body).orderId;

    const confirmRes = authedPatch(token, `${BASE}/orders/${orderId}/confirm`);
    check(confirmRes, { "confirm 204": (r) => r.status === 204 });

    const dispatchRes = authedPatch(token, `${BASE}/orders/${orderId}/dispatch`);
    check(dispatchRes, { "dispatch 204": (r) => r.status === 204 });
  });
}

// ── HTTP helpers ──────────────────────────────────────────────────────────────

function login(username, password) {
  const res = http.post(
    `${BASE}/login`,
    JSON.stringify({ username, password }),
    { headers: { "Content-Type": "application/json" } }
  );
  loginLatency.add(res.timings.duration);
  check(res, { "login 200": (r) => r.status === 200 });
  return JSON.parse(res.body).token;
}

function placeOrder(token, productId, stockLotId, quantity) {
  return http.post(
    `${BASE}/orders`,
    JSON.stringify({
      customerId: 1,
      notes:      "k6 load test order",
      lines:      [{ productId, stockLotId, quantity, unitPrice: 30.0 }],
    }),
    { headers: authedJson(token) }
  );
}

function authedGet(token, url) {
  return http.get(url, { headers: { Authorization: `Bearer ${token}` } });
}

function authedPatch(token, url) {
  return http.patch(url, null, { headers: { Authorization: `Bearer ${token}` } });
}

function authedDelete(token, url) {
  return http.del(url, null, { headers: { Authorization: `Bearer ${token}` } });
}

function authedJson(token) {
  return { "Content-Type": "application/json", Authorization: `Bearer ${token}` };
}
