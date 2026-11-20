import http from 'k6/http';
import { check } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginMember } from '../../../lib/auth.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 20);

export const options = {
  scenarios: {
    rooms_get_types_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 50,
      maxVUs: 200,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:rooms_get_types}': ['p(95)<300'],
    'http_req_failed{endpoint:rooms_get_types}': ['rate<0.01'],
    'http_reqs{endpoint:rooms_get_types}': ['count>0'],
  },
};

export function setup() {
  return {
    token: loginMember(BASE_URL),
  };
}

export default function (data) {
  const url = buildUrl(BASE_URL, endpoints.roomTypes);

  const res = http.get(url, {
    headers: {
      Authorization: `Bearer ${data.token}`,
    },
    tags: {
      endpoint: 'rooms_get_types',
    },
  });

  check(res, {
    'GET /api/rooms/types status 200': r => r.status === 200,
    'GET /api/rooms/types returns array': r => {
      if (r.status !== 200 || !r.body) return false;

      try {
        return Array.isArray(r.json());
      } catch {
        return false;
      }
    },
    'GET /api/rooms/types duration < 300ms': r => r.timings.duration < 300,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'rooms-get-types', 'rooms_get_types');
}