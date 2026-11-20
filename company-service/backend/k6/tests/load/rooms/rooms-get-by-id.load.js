import http from 'k6/http';
import { check } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginMember } from '../../../lib/auth.js';
import { getOrCreateRoom } from '../../../lib/rooms.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 20);

export const options = {
  scenarios: {
    rooms_get_by_id_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 50,
      maxVUs: 200,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:rooms_get_by_id}': ['p(95)<300'],
    'http_req_failed{endpoint:rooms_get_by_id}': ['rate<0.01'],
    'http_reqs{endpoint:rooms_get_by_id}': ['count>0'],
  },
};

export function setup() {
  const token = loginMember(BASE_URL);
  const roomId = getOrCreateRoom(BASE_URL, token);

  return {
    token,
    roomId,
  };
}

export default function (data) {
  const url = buildUrl(BASE_URL, endpoints.roomById, {
    id: data.roomId,
  });

  const res = http.get(url, {
    headers: {
      Authorization: `Bearer ${data.token}`,
    },
    tags: {
      endpoint: 'rooms_get_by_id',
    },
  });

  check(res, {
    'GET /api/rooms/{id} status 200': r => r.status === 200,
    'GET /api/rooms/{id} duration < 300ms': r => r.timings.duration < 300,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'rooms-get-by-id', 'rooms_get_by_id');
}