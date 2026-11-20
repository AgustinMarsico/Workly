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
    invitations_get_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 90,
      maxVUs: 300,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:invitations_get}': ['p(95)<300'],
    'http_req_failed{endpoint:invitations_get}': ['rate<0.01'],
    'http_reqs{endpoint:invitations_get}': ['count>0'],
  },
};

export function setup() {
  if (!COMPANY_ID) {
    fail('COMPANY_ID o INVITATION_COMPANY_ID es requerido para obtener invitaciones.');
  }

  const adminToken = loginAdmin(BASE_URL);

  // Create invitation in setup to get invitation ID
  const unique = `${Date.now()}-setup`;
  const payload = {
    userEmail: `k6.invitation.${unique}@${INVITATION_EMAIL_DOMAIN}`,
    companyId: COMPANY_ID,
  };

  const url = buildUrl(BASE_URL, endpoints.invitations);
  const res = http.post(url, JSON.stringify(payload), {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${adminToken}`,
    },
  });

  if (res.status !== 200) {
    fail(`Failed to create invitation for setup. Status: ${res.status}`);
  }

  let invitationId;
  try {
    const body = res.json();
    invitationId = body.id;
  } catch (e) {
    fail(`Failed to parse invitation response: ${e.message}`);
  }

  if (!invitationId) {
    fail('Failed to get invitation ID from response');
  }

  return {
    invitationId,
  };
}

export default function (data) {
  const url = buildUrl(BASE_URL, endpoints.invitationById, { id: data.invitationId });

  const res = http.get(url, {
    tags: {
      endpoint: 'invitations_get',
    },
  });

  check(res, {
    'GET /api/invitations/{id} status 200': r => r.status === 200,
    'GET /api/invitations/{id} returns email': r => {
      if (r.status !== 200 || !r.body) return false;

      try {
        const body = r.json();
        return body.email !== undefined;
      } catch {
        return false;
      }
    },
    'GET /api/invitations/{id} returns companyName': r => {
      if (r.status !== 200 || !r.body) return false;

      try {
        const body = r.json();
        return body.companyName !== undefined;
      } catch {
        return false;
      }
    },
    'GET /api/invitations/{id} duration < 300ms': r => r.timings.duration < 300,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'invitations-get', 'invitations_get');
}
