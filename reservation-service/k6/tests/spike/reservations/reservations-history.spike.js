import http from 'k6/http';
import { check } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginMember } from '../../../lib/auth.js';
import { getOrCreateRoom } from '../../../lib/rooms.js';
import { createApiKey, apiKeyHeaders } from '../../../lib/apiKey.js';
import { buildSummary } from '../../../lib/summary.js';

const MAIN_SERVICE_URL = __ENV.MAIN_SERVICE_URL || 'http://localhost:5000';
const RESERVATION_SERVICE_URL = __ENV.RESERVATION_SERVICE_URL || 'http://localhost:8081';

export const options = {
  scenarios: {
    reservations_history_spike_test: {
      executor: 'ramping-arrival-rate',
      startRate: Number(__ENV.SPIKE_START_RATE || 5),
      timeUnit: '1s',
      preAllocatedVUs: Number(__ENV.SPIKE_PRE_ALLOCATED_VUS || 100),
      maxVUs: Number(__ENV.SPIKE_MAX_VUS || 400),
      stages: [
        { target: Number(__ENV.SPIKE_RATE || 150), duration: __ENV.SPIKE_RAMP_UP || '20s' },
        { target: Number(__ENV.SPIKE_RATE || 150), duration: __ENV.SPIKE_HOLD || '40s' },
        { target: Number(__ENV.SPIKE_START_RATE || 5), duration: __ENV.SPIKE_RAMP_DOWN || '20s' },
      ],
    },
  },
  thresholds: { 'http_req_duration{endpoint:reservations_history_spike}': ['p(95)<500'], 'http_req_failed{endpoint:reservations_history_spike}': ['rate<0.01'] },
};

export function setup() {
  const token = loginMember(MAIN_SERVICE_URL);
  const roomId = getOrCreateRoom(MAIN_SERVICE_URL, token);
  const apiKey = createApiKey(MAIN_SERVICE_URL, token, `k6-history-spike-${Date.now()}`);
  return { roomId, apiKey };
}

export default function (data) {
  const url = buildUrl(RESERVATION_SERVICE_URL, endpoints.reservationHistory, {}, {
    roomId: data.roomId,
    fromUtc: '2029-01-01T00:00:00Z',
    toUtc: '2036-01-01T00:00:00Z',
  });
  const res = http.get(url, { headers: { ...apiKeyHeaders(data.apiKey) }, tags: { endpoint: 'reservations_history_spike' } });
  check(res, { 'GET /api/reservations/history spike status 200': r => r.status === 200 });
}

export function handleSummary(data) {
  return buildSummary(data, 'reservations-history-spike', 'reservations_history_spike', { latencyThresholdMs: 500 });
}
