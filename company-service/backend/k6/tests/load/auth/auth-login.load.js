import http from 'k6/http';
import { check } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || 'admin@saas.com';
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || 'Password1';

export const options = {
  scenarios: {
    auth_login_load_test: {
      executor: 'constant-arrival-rate',
      rate: 20,
      timeUnit: '1s',
      duration: '1m',
      preAllocatedVUs: 50,
      maxVUs: 200,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:auth_login}': ['p(95)<300'],
    'http_req_failed{endpoint:auth_login}': ['rate<0.01'],
    'http_reqs{endpoint:auth_login}': ['count>0'],
  },
};

export default function () {
  const url = buildUrl(BASE_URL, endpoints.login);

  const res = http.post(
    url,
    JSON.stringify({
      email: ADMIN_EMAIL,
      password: ADMIN_PASSWORD,
    }),
    {
      headers: {
        'Content-Type': 'application/json',
      },
      tags: {
        endpoint: 'auth_login',
      },
    }
  );

  check(res, {
    'POST /api/auth/login status 200': r => r.status === 200,
    'POST /api/auth/login returns token': r => {
      if (r.status !== 200 || !r.body) {
        return false;
      }

      try {
        return !!r.json('token');
      } catch {
        return false;
      }
    },
    'POST /api/auth/login duration < 300ms': r => r.timings.duration < 300,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'auth-login', 'auth_login');
}