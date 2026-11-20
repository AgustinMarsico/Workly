import http from 'k6/http';
import { check } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginAdmin } from '../../../lib/auth.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 20);

export const options = {
  scenarios: {
    users_get_all_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 50,
      maxVUs: 200,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:users_get_all}': ['p(95)<300'],
    'http_req_failed{endpoint:users_get_all}': ['rate<0.01'],
    'http_reqs{endpoint:users_get_all}': ['count>0'],
  },
};

export function setup() {
  const token = loginAdmin(BASE_URL);

  return {
    token,
  };
}

export default function (data) {
  const url = buildUrl(BASE_URL, endpoints.users);

  const res = http.get(url, {
    headers: {
      Authorization: `Bearer ${data.token}`,
    },
    tags: {
      endpoint: 'users_get_all',
    },
  });

  check(res, {
    'GET /api/users status 200': r => r.status === 200,
    'GET /api/users duration < 300ms': r => r.timings.duration < 300,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'users-get-all', 'users_get_all');
}