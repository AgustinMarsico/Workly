import http from 'k6/http';
import { check, fail } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginMember } from '../../../lib/auth.js';
import { getOrCreateRoom } from '../../../lib/rooms.js';
import { createApiKey, apiKeyHeaders } from '../../../lib/apiKey.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 20);

export const options = {
  scenarios: {
    rooms_top_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 50,
      maxVUs: 300,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:rooms_top}': ['p(95)<300'],
    'http_req_failed{endpoint:rooms_top}': ['rate<0.01'],
    'http_reqs{endpoint:rooms_top}': ['count>0'],
  },
};

export function setup() {
  const memberToken = loginMember(BASE_URL);

  getOrCreateRoom(BASE_URL, memberToken);

  const apiKey = createApiKey(
    BASE_URL,
    memberToken,
    `k6-rooms-top-${Date.now()}`
  );

  const url = buildUrl(BASE_URL, endpoints.topRooms);

  const smokeRes = http.get(url, {
    headers: {
      ...apiKeyHeaders(apiKey),
    },
    timeout: '10s',
    tags: {
      endpoint: 'setup_rooms_top_smoke',
    },
  });

  if (smokeRes.status !== 200) {
    fail(`GET /api/rooms/top no responde 200 antes de la carga. Status: ${smokeRes.status}. Body: ${smokeRes.body}`);
  }

  return {
    apiKey,
  };
}

export default function (data) {
  const url = buildUrl(BASE_URL, endpoints.topRooms);

  const res = http.get(url, {
    headers: {
      ...apiKeyHeaders(data.apiKey),
    },
    timeout: '10s',
    tags: {
      endpoint: 'rooms_top',
    },
  });

  check(res, {
    'GET /api/rooms/top status 200': r => r.status === 200,
    'GET /api/rooms/top duration < 300ms': r => r.timings.duration < 300,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'rooms-top', 'rooms_top');
}