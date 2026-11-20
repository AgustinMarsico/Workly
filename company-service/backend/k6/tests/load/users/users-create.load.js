import http from 'k6/http';
import { check, fail } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginAdmin } from '../../../lib/auth.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 20);

const CREATE_USER_ROLE = __ENV.CREATE_USER_ROLE || 'Member';
const COMPANY_ID = __ENV.COMPANY_ID || null;

export const options = {
  scenarios: {
    users_create_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 50,
      maxVUs: 300,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:users_create}': ['p(95)<300'],
    'http_req_failed{endpoint:users_create}': ['rate<0.01'],
    'http_reqs{endpoint:users_create}': ['count>0'],
  },
};

export function setup() {
  const token = loginAdmin(BASE_URL);

  if (CREATE_USER_ROLE === 'Member' && !COMPANY_ID) {
    fail('COMPANY_ID es requerido para crear usuarios Member. Definilo en k6/.env o pasalo como variable de entorno.');
  }

  return {
    token,
    companyId: COMPANY_ID,
  };
}

export default function (data) {
  const unique = `${__VU}-${__ITER}-${Date.now()}`;

  const payload = {
    email: `load.user.${unique}@workly.test`,
    password: 'Password1',
    role: CREATE_USER_ROLE,
    companyId: CREATE_USER_ROLE === 'Member' ? data.companyId : null,
  };

  const url = buildUrl(BASE_URL, endpoints.users);

  const res = http.post(url, JSON.stringify(payload), {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${data.token}`,
    },
    tags: {
      endpoint: 'users_create',
    },
  });

  check(res, {
    'POST /api/users status 201': r => r.status === 201,
    'POST /api/users duration < 300ms': r => r.timings.duration < 300,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'users-create', 'users_create');
}