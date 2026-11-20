import http from 'k6/http';
import { check, fail } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { buildSpikeScenario } from '../../../config/scenarios.js';
import { loginMember } from '../../../lib/auth.js';
import { getOrCreateRoom } from '../../../lib/rooms.js';
import { createApiKey, apiKeyHeaders } from '../../../lib/apiKey.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export const options = {
  scenarios: {
    rooms_top_spike_test: buildSpikeScenario({
      startRate: 5,
      peakRate: 120,
      preAllocatedVUs: 80,
      maxVUs: 350,
    }),
  },
  thresholds: {
    'http_req_duration{endpoint:rooms_top_spike}': ['p(95)<500'],
    'http_req_failed{endpoint:rooms_top_spike}': ['rate<0.01'],
    'http_reqs{endpoint:rooms_top_spike}': ['count>0'],
  },
};

export function setup() {
  const memberToken = loginMember(BASE_URL);

  getOrCreateRoom(BASE_URL, memberToken);

  const apiKey = createApiKey(
    BASE_URL,
    memberToken,
    `k6-rooms-top-spike-${Date.now()}`
  );

  const url = buildUrl(BASE_URL, endpoints.topRooms);

  const smokeRes = http.get(url, {
    headers: {
      ...apiKeyHeaders(apiKey),
    },
    timeout: '10s',
    tags: {
      endpoint: 'setup_rooms_top_spike_smoke',
    },
  });

  if (smokeRes.status !== 200) {
    fail(`GET /api/rooms/top no responde 200 antes del spike. Status: ${smokeRes.status}. Body: ${smokeRes.body}`);
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
      endpoint: 'rooms_top_spike',
    },
  });

  check(res, {
    'GET /api/rooms/top status 200': r => r.status === 200,
    'GET /api/rooms/top spike duration < 500ms': r => r.timings.duration < 500,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'rooms-top-spike', 'rooms_top_spike', {
    latencyThresholdMs: 500,
  });
}
