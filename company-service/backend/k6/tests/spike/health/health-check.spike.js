import http from 'k6/http';
import { check } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { buildSpikeScenario } from '../../../config/scenarios.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export const options = {
  scenarios: {
    health_check_spike_test: buildSpikeScenario({
      startRate: 10,
      peakRate: 250,
      preAllocatedVUs: 150,
      maxVUs: 500,
    }),
  },
  thresholds: {
    'http_req_duration{endpoint:health_check_spike}': ['p(95)<400'],
    'http_req_failed{endpoint:health_check_spike}': ['rate<0.01'],
    'http_reqs{endpoint:health_check_spike}': ['count>0'],
  },
};

export default function () {
  const url = buildUrl(BASE_URL, endpoints.health);

  const res = http.get(url, {
    tags: {
      endpoint: 'health_check_spike',
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
    'GET /health spike duration < 400ms': r => r.timings.duration < 400,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'health-check-spike', 'health_check_spike', {
    latencyThresholdMs: 400,
  });
}
