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
    companies_get_all_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 50,
      maxVUs: 200,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:companies_get_all}': ['p(95)<300'],
    'http_req_failed{endpoint:companies_get_all}': ['rate<0.01'],
    'http_reqs{endpoint:companies_get_all}': ['count>0'],
  },
};

export function setup() {
  const token = loginAdmin(BASE_URL);

  return {
    token,
  };
}

export default function (data) {
  const url = buildUrl(BASE_URL, endpoints.companies);

  const res = http.get(url, {
    headers: {
      Authorization: `Bearer ${data.token}`,
    },
    tags: {
      endpoint: 'companies_get_all',
    },
  });

  check(res, {
    'GET /api/companies status 200': r => r.status === 200,
    'GET /api/companies duration < 300ms': r => r.timings.duration < 300,
    'GET /api/companies returns array': r => {
      if (r.status !== 200 || !r.body) {
        return false;
      }

      try {
        return Array.isArray(r.json());
      } catch {
        return false;
      }
    },
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'companies-get-all', 'companies_get_all');
}