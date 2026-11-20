import http from 'k6/http';
import { check } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginMember } from '../../../lib/auth.js';
import { buildSummary } from '../../../lib/summary.js';

const MAIN_SERVICE_URL = __ENV.MAIN_SERVICE_URL || 'http://localhost:5000';
const RESERVATION_SERVICE_URL = __ENV.RESERVATION_SERVICE_URL || 'http://localhost:8081';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 20);

export const options = {
  scenarios: { reservations_upcoming_stats_load_test: { executor: 'constant-arrival-rate', rate: RATE, timeUnit: '1s', duration: TEST_DURATION, preAllocatedVUs: 50, maxVUs: 200 } },
  thresholds: { 'http_req_duration{endpoint:reservations_upcoming_stats}': ['p(95)<300'], 'http_req_failed{endpoint:reservations_upcoming_stats}': ['rate<0.01'] },
};

export function setup() {
  return { token: loginMember(MAIN_SERVICE_URL) };
}

export default function (data) {
  const res = http.get(buildUrl(RESERVATION_SERVICE_URL, endpoints.upcomingReservationStats), { headers: { Authorization: `Bearer ${data.token}` }, tags: { endpoint: 'reservations_upcoming_stats' } });
  check(res, { 'GET /api/reservations/upcoming/stats status 200': r => r.status === 200 });
}

export function handleSummary(data) {
  return buildSummary(data, 'reservations-upcoming-stats', 'reservations_upcoming_stats');
}
