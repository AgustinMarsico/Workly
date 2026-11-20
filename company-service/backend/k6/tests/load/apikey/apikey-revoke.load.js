import http from 'k6/http';
import { check } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginMember } from '../../../lib/auth.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 20);

export const options = {
  scenarios: {
    apikey_revoke_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 50,
      maxVUs: 300,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:apikey_revoke}': ['p(95)<300'],
    'http_req_failed{endpoint:apikey_revoke}': ['rate<0.01'],
    'http_reqs{endpoint:apikey_revoke}': ['count>0'],
  },
};

export function setup() {
  const token = loginMember(BASE_URL);

  return {
    token,
  };
}

export default function (data) {
  const apiKeyId = createApiKeyForRevoke(data.token);

  const url = buildUrl(BASE_URL, endpoints.apiKeyById, {
    id: apiKeyId,
  });

  const res = http.del(url, null, {
    headers: {
      Authorization: `Bearer ${data.token}`,
    },
    tags: {
      endpoint: 'apikey_revoke',
    },
  });

  check(res, {
    'DELETE /api/api-keys/{id} status 204': r => r.status === 204,
    'DELETE /api/api-keys/{id} duration < 300ms': r => r.timings.duration < 300,
  });
}

function createApiKeyForRevoke(token) {
  const unique = `${__VU}-${__ITER}-${Date.now()}`;

  const payload = {
    name: `k6-revoke-api-key-${unique}`,
  };

  const url = buildUrl(BASE_URL, endpoints.apiKeys);

  const res = http.post(url, JSON.stringify(payload), {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    tags: {
      endpoint: 'setup_apikey_create_for_revoke',
    },
  });

  const ok = check(res, {
    'setup POST /api/api-keys status 201': r => r.status === 201,
    'setup POST /api/api-keys returns id': r => {
      if (r.status !== 201 || !r.body) {
        return false;
      }

      try {
        const body = r.json();
        return !!(body.id || body.Id);
      } catch {
        return false;
      }
    },
  });

  if (!ok) {
    throw new Error(`No se pudo crear API key para revocar. Status: ${res.status}. Body: ${res.body}`);
  }

  const body = res.json();
  return body.id || body.Id;
}

export function handleSummary(data) {
  return buildSummary(data, 'apikey-revoke', 'apikey_revoke');
}