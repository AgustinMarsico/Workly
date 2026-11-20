import http from 'k6/http';
import { check, fail } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginAdmin } from '../../../lib/auth.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 20);

export const options = {
  scenarios: {
    companies_get_by_id_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 50,
      maxVUs: 200,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:companies_get_by_id}': ['p(95)<300'],
    'http_req_failed{endpoint:companies_get_by_id}': ['rate<0.01'],
    'http_reqs{endpoint:companies_get_by_id}': ['count>0'],
  },
};

export function setup() {
  const token = loginAdmin(BASE_URL);

  const companiesUrl = buildUrl(BASE_URL, endpoints.companies);

  const companiesRes = http.get(companiesUrl, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
    tags: {
      endpoint: 'setup_companies_get_all',
    },
  });

  check(companiesRes, {
    'setup GET /api/companies status 200': r => r.status === 200,
  });

  if (companiesRes.status !== 200 || !companiesRes.body) {
    fail(`No se pudieron obtener empresas. Status: ${companiesRes.status}. Body: ${companiesRes.body}`);
  }

  const companies = companiesRes.json();

  if (!Array.isArray(companies) || companies.length === 0) {
    fail('No hay empresas existentes para probar GET /api/companies/{id}');
  }

  const companyId = companies[0].id || companies[0].Id;

  if (!companyId) {
    fail(`La empresa obtenida no tiene id. Body: ${JSON.stringify(companies[0])}`);
  }

  return {
    token,
    companyId,
  };
}

export default function (data) {
  const url = buildUrl(BASE_URL, endpoints.companyById, {
    id: data.companyId,
  });

  const res = http.get(url, {
    headers: {
      Authorization: `Bearer ${data.token}`,
    },
    tags: {
      endpoint: 'companies_get_by_id',
    },
  });

  check(res, {
    'GET /api/companies/{id} status 200': r => r.status === 200,
    'GET /api/companies/{id} duration < 300ms': r => r.timings.duration < 300,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'companies-get-by-id', 'companies_get_by_id');
}