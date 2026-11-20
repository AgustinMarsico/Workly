import http from 'k6/http';
import { check } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginMember } from '../../../lib/auth.js';
import { getOrCreateRoom } from '../../../lib/rooms.js';
import { reservationTimeSlot } from '../../../lib/reservations.js';
import { buildSummary } from '../../../lib/summary.js';

const MAIN_SERVICE_URL = __ENV.MAIN_SERVICE_URL || 'http://localhost:5000';
const RESERVATION_SERVICE_URL = __ENV.RESERVATION_SERVICE_URL || 'http://localhost:8081';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 20);

export const options = {
  scenarios: { reservations_create_load_test: { executor: 'constant-arrival-rate', rate: RATE, timeUnit: '1s', duration: TEST_DURATION, preAllocatedVUs: 50, maxVUs: 300 } },
  thresholds: {
    'http_req_duration{endpoint:reservations_create}': ['p(95)<300'],
    'http_req_failed{endpoint:reservations_create}': ['rate<0.01'],
  },
};

export function setup() {
  const token = loginMember(MAIN_SERVICE_URL);
  return { token, roomId: getOrCreateRoom(MAIN_SERVICE_URL, token) };
}

export default function (data) {
  const { start, end } = reservationTimeSlot();
  const res = http.post(buildUrl(RESERVATION_SERVICE_URL, endpoints.reservations), JSON.stringify({ roomId: data.roomId, start, end }), {
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${data.token}` },
    tags: { endpoint: 'reservations_create' },
  });

  check(res, { 'POST /api/reservations status 201': r => r.status === 201 });
}

export function handleSummary(data) {
  return buildSummary(data, 'reservations-create', 'reservations_create');
}
