import http from 'k6/http';
import { check } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginAdmin } from '../../../lib/auth.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 20);

const SAMPLE_BASE64_LOGO =
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=';

export const options = {
  scenarios: {
    companies_create_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 50,
      maxVUs: 300,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:companies_create}': ['p(95)<300'],
    'http_req_failed{endpoint:companies_create}': ['rate<0.01'],
    'http_reqs{endpoint:companies_create}': ['count>0'],
  },
};

export function setup() {
  const token = loginAdmin(BASE_URL);

  return {
    token,
  };
}

export default function (data) {
  const unique = `${__VU}-${__ITER}-${Date.now()}`;

  const payload = {
    name: `K6 Company ${unique}`,
    address: {
      street: 'Cuareim',
      number: 1451,
      city: 'Montevideo',
      state: 'Montevideo',
      zipCode: '11100',
      country: 'Uruguay',
    },
    website: `https://k6-company-${unique}.com`,
    base64Logo: SAMPLE_BASE64_LOGO,
  };

  const url = buildUrl(BASE_URL, endpoints.companies);

  const res = http.post(url, JSON.stringify(payload), {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${data.token}`,
    },
    tags: {
      endpoint: 'companies_create',
    },
  });

  check(res, {
    'POST /api/companies status 200': r => r.status === 200,
    'POST /api/companies returns id': r => {
      if (r.status !== 200 || !r.body) {
        return false;
      }

      try {
        const body = r.json();
        return !!(body.id || body.Id);
      } catch {
        return false;
      }
    },
    'POST /api/companies duration < 300ms': r => r.timings.duration < 300,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'companies-create', 'companies_create');
}