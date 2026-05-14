/**
 * Cold-start measurement script — Functions only.
 *
 * Fires one request immediately after the Functions app has been idle
 * (consumption plan auto-pauses after ~5 minutes of inactivity).
 *
 * Usage:
 *   1. Leave the Functions app idle for at least 5 minutes.
 *   2. Run:  k6 run -e BASE_URL=https://ordle-functions.azurewebsites.net/api -e FUNCTIONS_KEY=<key> benchmarks/cold-start.js
 *   3. Compare http_req_duration of iteration 0 (cold) vs iterations 1+ (warm).
 *
 * The output shows a histogram of latencies — the first request is the cold start,
 * subsequent requests hit a warm instance.
 */

import http from 'k6/http'
import { check } from 'k6'

const BASE_URL      = __ENV.BASE_URL      || 'http://localhost:7071/api'
const FUNCTIONS_KEY = __ENV.FUNCTIONS_KEY || ''
const authHeaders   = FUNCTIONS_KEY ? { 'x-functions-key': FUNCTIONS_KEY } : {}

export const options = {
  scenarios: {
    cold_start_then_warm: {
      executor: 'shared-iterations',
      vus: 1,
      iterations: 10,
    },
  },
  // No thresholds — this script is purely for observation, not pass/fail
}

export default function () {
  const res = http.get(`${BASE_URL}/health`, { headers: authHeaders })
  check(res, { 'health 200': r => r.status === 200 })
  // No sleep — send requests back-to-back to see warm-instance latency
}
