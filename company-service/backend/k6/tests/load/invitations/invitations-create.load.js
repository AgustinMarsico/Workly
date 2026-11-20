import http from 'k6/http';
import { check, fail } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginAdmin } from '../../../lib/auth.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 20);

const COMPANY_ID = __ENV.INVITATION_COMPANY_ID || __ENV.COMPANY_ID || null;
const INVITATION_EMAIL_DOMAIN = __ENV.INVITATION_EMAIL_DOMAIN || 'workly.test';

export const options = {
  scenarios: {
    invitations_create_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 50,
      maxVUs: 300,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:invitations_create}': ['p(95)<300'],
    'http_req_failed{endpoint:invitations_create}': ['rate<0.01'],
    'http_reqs{endpoint:invitations_create}': ['count>0'],
  },
};

export function setup() {
  const token = loginAdmin(BASE_URL);

  if (!COMPANY_ID) {
    fail('COMPANY_ID o INVITATION_COMPANY_ID es requerido para crear invitaciones.');
  }

  return {
    token,
    companyId: COMPANY_ID,
  };
}

export default function (data) {
  const unique = `${__VU}-${__ITER}-${Date.now()}`;

  const payload = {
    userEmail: `k6.invitation.${unique}@${INVITATION_EMAIL_DOMAIN}`,
    companyId: data.companyId,
  };

  const url = buildUrl(BASE_URL, endpoints.invitations);

  const res = http.post(url, JSON.stringify(payload), {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${data.token}`,
    },
    tags: {
      endpoint: 'invitations_create',
    },
  });

  check(res, {
    'POST /api/invitations status 200': r => r.status === 200,
    'POST /api/invitations returns body': r => r.status === 200 && !!r.body,
    'POST /api/invitations duration < 300ms': r => r.timings.duration < 300,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'invitations-create', 'invitations_create');
}