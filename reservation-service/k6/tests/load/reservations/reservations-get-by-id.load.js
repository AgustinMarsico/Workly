import http from 'k6/http';
import { check } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginMember } from '../../../lib/auth.js';
import { getOrCreateRoom } from '../../../lib/rooms.js';
import { createReservationForTest } from '../../../lib/reservations.js';
import { buildSummary } from '../../../lib/summary.js';

const MAIN_SERVICE_URL = __ENV.MAIN_SERVICE_URL || 'http://localhost:5000';
const RESERVATION_SERVICE_URL = __ENV.RESERVATION_SERVICE_URL || 'http://localhost:8081';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 20);

export const options = {
  scenarios: { reservations_get_by_id_load_test: { executor: 'constant-arrival-rate', rate: RATE, timeUnit: '1s', duration: TEST_DURATION, preAllocatedVUs: 50, maxVUs: 200 } },
  thresholds: { 'http_req_duration{endpoint:reservations_get_by_id}': ['p(95)<300'], 'http_req_failed{endpoint:reservations_get_by_id}': ['rate<0.01'] },
};

export function setup() {
  const token = loginMember(MAIN_SERVICE_URL);
  return { token, roomId: getOrCreateRoom(MAIN_SERVICE_URL, token) };
}

export default function (data) {
  const id = createReservationForTest(RESERVATION_SERVICE_URL, data.token, data.roomId, 'setup_reservation_create_for_get_by_id');
  const res = http.get(buildUrl(RESERVATION_SERVICE_URL, endpoints.reservationById, { id }), {
    headers: { Authorization: `Bearer ${data.token}` },
    tags: { endpoint: 'reservations_get_by_id' },
  });
  check(res, { 'GET /api/reservations/{id} status 200': r => r.status === 200 });
}

export function handleSummary(data) {
  return buildSummary(data, 'reservations-get-by-id', 'reservations_get_by_id');
}
