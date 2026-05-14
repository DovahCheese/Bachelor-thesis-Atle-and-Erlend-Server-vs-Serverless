/**
 * Ordle load test — runs against one backend at a time.
 *
 * Usage:
 *   # App Service — no key needed
 *   k6 run -e BASE_URL=https://ordle-webapi-a3dhayeae7f9fbg3.westeurope-01.azurewebsites.net/api -e SCENARIO=baseline benchmarks/load-test.js
 *
 *   # Azure Functions — pass the default function key from the portal (App keys → default)
 *   k6 run -e BASE_URL=https://ordle-functions.azurewebsites.net/api -e FUNCTIONS_KEY=<key> -e SCENARIO=rampup benchmarks/load-test.js
 *
 * Scenarios (set via -e SCENARIO=<name>):
 *   baseline  — 1 VU, 100 requests. Establishes a clean latency floor.
 *   rampup    — 0 → 50 VUs over 2 min, hold 3 min, ramp down 1 min.
 *   spike     — instant 100 VUs for 30 s. Highlights cold-start behaviour on Functions.
 *   soak      — 10 VUs for 30 min. Checks sustained throughput and memory drift.
 *
 * Metrics captured by k6:
 *   http_req_duration   — end-to-end latency (p50 / p95 / p99)
 *   http_reqs           — total requests and rate (req/s)
 *   http_req_failed     — error rate (5xx and network failures)
 *
 * Thresholds (thesis SLOs):
 *   p95 < 800 ms, error rate < 1 %
 */

import http from 'k6/http'
import { check, sleep } from 'k6'
import { Trend, Counter } from 'k6/metrics'

// ── Custom metrics ────────────────────────────────────────────────────────────
const guessLatency = new Trend('guess_latency', true)
const gameLatency  = new Trend('game_latency',  true)
const errorCount   = new Counter('error_count')

// ── Config ────────────────────────────────────────────────────────────────────
const BASE_URL      = __ENV.BASE_URL      || 'http://localhost:7071/api'
const SCENARIO      = __ENV.SCENARIO      || 'baseline'
// Required when targeting AzureFunctions (AuthorizationLevel.Function).
// Leave unset for WebApi (App Service has no function-key auth).
const FUNCTIONS_KEY = __ENV.FUNCTIONS_KEY || ''

const authHeaders = FUNCTIONS_KEY
  ? { 'x-functions-key': FUNCTIONS_KEY }
  : {}

const SCENARIOS = {
  baseline: {
    executor: 'shared-iterations',
    vus: 1,
    iterations: 100,
    maxDuration: '5m',
  },
  rampup: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '2m', target: 50 },
      { duration: '3m', target: 50 },
      { duration: '1m', target: 0  },
    ],
    gracefulRampDown: '30s',
  },
  spike: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '5s',  target: 100 },
      { duration: '30s', target: 100 },
      { duration: '5s',  target: 0   },
    ],
    gracefulRampDown: '10s',
  },
  soak: {
    executor: 'constant-vus',
    vus: 10,
    duration: '30m',
  },
}

export const options = {
  scenarios: {
    test: SCENARIOS[SCENARIO] || SCENARIOS.baseline,
  },
  thresholds: {
    http_req_duration: ['p(95)<800'],
    http_req_failed:   ['rate<0.01'],
  },
}

// ── Helpers ───────────────────────────────────────────────────────────────────
const JSON_HEADERS = { 'Content-Type': 'application/json', ...authHeaders }

function checkOk(res, label) {
  const ok = check(res, { [`${label} status 2xx`]: r => r.status >= 200 && r.status < 300 })
  if (!ok) errorCount.add(1)
  return ok
}

// ── Main test function ────────────────────────────────────────────────────────
export default function () {
  // 1. GET daily game
  const getRes = http.get(`${BASE_URL}/game`, { headers: authHeaders })
  gameLatency.add(getRes.timings.duration)
  checkOk(getRes, 'GET /game')

  let gameId
  if (getRes.status === 200) {
    try {
      gameId = JSON.parse(getRes.body).gameId
    } catch (_) {}
  }

  // 2. POST a guess (valid 5-letter Norwegian word)
  if (gameId) {
    const guessRes = http.post(
      `${BASE_URL}/game/guess`,
      JSON.stringify({ gameId, guess: 'BAKER' }),
      { headers: JSON_HEADERS, responseCallback: http.expectedStatuses(200, 400, 409) },
    )
    guessLatency.add(guessRes.timings.duration)
    check(guessRes, { 'POST /game/guess acceptable': r => r.status === 200 || r.status === 409 || r.status === 400 })
  }

  // 3. Health check — no-op baseline endpoint
  const healthRes = http.get(`${BASE_URL}/health`, { headers: authHeaders })
  checkOk(healthRes, 'GET /health')

  sleep(1)
}
