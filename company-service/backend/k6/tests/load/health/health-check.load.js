import http from 'k6/http';
import { check } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 400);

export const options = {
  scenarios: {
    health_check_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 120,
      maxVUs: 400,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:health_check}': ['p(95)<300'],
    'http_req_failed{endpoint:health_check}': ['rate<0.01'],
    'http_reqs{endpoint:health_check}': ['count>0'],
  },
};

export default function () {
  const url = buildUrl(BASE_URL, endpoints.health);

  const res = http.get(url, {
    tags: {
      endpoint: 'health_check',
    },
  });

  check(res, {
    'GET /health status 200': r => r.status === 200,
    'GET /health returns status healthy': r => {
      if (r.status !== 200 || !r.body) return false;

      try {
        const body = r.json();
        return body.status === 'healthy';
      } catch {
        return false;
      }
    },
    'GET /health duration < 100ms': r => r.timings.duration < 100,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'health-check', 'health_check');
}
