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
  setupTimeout: '10m',
  teardownTimeout: '1m',
  scenarios: {
    invitations_accept_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 60,
      maxVUs: 200,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:invitations_accept}': ['p(95)<300'],
    'http_req_failed{endpoint:invitations_accept}': ['rate<0.01'],
    'http_reqs{endpoint:invitations_accept}': ['count>0'],
  },
};

export function setup() {
  if (!COMPANY_ID) {
    fail('COMPANY_ID o INVITATION_COMPANY_ID es requerido para aceptar invitaciones.');
  }

  const adminToken = loginAdmin(BASE_URL);

  return {
    adminToken,
  };
}

export default function (data) {
  // Generar email único para cada iteración
  const unique = `${__VU}_${__ITER}_${Date.now()}`;
  const email = `k6.invitation.${unique}@${INVITATION_EMAIL_DOMAIN}`;

  // 1. Crear invitación
  const invitationsUrl = buildUrl(BASE_URL, endpoints.invitations);
  const createPayload = {
    userEmail: email,
    companyId: COMPANY_ID,
  };

  const createRes = http.post(invitationsUrl, JSON.stringify(createPayload), {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${data.adminToken}`,
    },
    tags: {
      endpoint: 'setup_create_invitation',
    },
  });

  if (createRes.status !== 200) {
    fail(`Failed to create invitation: ${createRes.status} - ${createRes.body}`);
  }

  let invitationId;
  try {
    const createBody = createRes.json();
    invitationId = createBody.id;
  } catch (e) {
    fail(`Failed to parse create invitation response: ${e.message}`);
  }

  if (!invitationId) {
    fail('No invitation ID returned from create');
  }

  // 2. Aceptar invitación
  const password = `TestPassword123!`;

  const acceptPayload = {
    invitationId: invitationId,
    hashedPassword: password,
  };

  const acceptUrl = buildUrl(BASE_URL, endpoints.invitationsAccept);

  const acceptRes = http.post(acceptUrl, JSON.stringify(acceptPayload), {
    headers: {
      'Content-Type': 'application/json',
    },
    tags: {
      endpoint: 'invitations_accept',
    },
  });

  check(acceptRes, {
    'POST /api/invitations/accept status 200': r => r.status === 200,
    'POST /api/invitations/accept returns user': r => {
      if (r.status !== 200 || !r.body) return false;

      try {
        const body = r.json();
        return body.id !== undefined && body.email !== undefined;
      } catch {
        return false;
      }
    },
    'POST /api/invitations/accept returns role': r => {
      if (r.status !== 200 || !r.body) return false;

      try {
        const body = r.json();
        return body.role !== undefined;
      } catch {
        return false;
      }
    },
    'POST /api/invitations/accept returns companyId': r => {
      if (r.status !== 200 || !r.body) return false;

      try {
        const body = r.json();
        return body.companyId !== undefined;
      } catch {
        return false;
      }
    },
    'POST /api/invitations/accept duration < 500ms': r => r.timings.duration < 500,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'invitations-accept', 'invitations_accept');
}
