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
    rooms_create_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 50,
      maxVUs: 300,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:rooms_create}': ['p(95)<300'],
    'http_req_failed{endpoint:rooms_create}': ['rate<0.01'],
    'http_reqs{endpoint:rooms_create}': ['count>0'],
  },
};

export function setup() {
  return {
    token: loginMember(BASE_URL),
  };
}

export default function (data) {
  const unique = `${__VU}-${__ITER}-${Date.now()}`;

  const payload = {
    name: `Sala K6 ${unique}`,
    typeKey: 'Sala',
    maxCapacity: 8,
    equipment: 'TV, pizarra, videoconferencia',
  };

  const url = buildUrl(BASE_URL, endpoints.rooms);

  const res = http.post(url, JSON.stringify(payload), {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${data.token}`,
    },
    tags: {
      endpoint: 'rooms_create',
    },
  });

  check(res, {
    'POST /api/rooms status 201': r => r.status === 201,
    'POST /api/rooms returns id': r => {
      if (r.status !== 201 || !r.body) return false;

      try {
        const body = r.json();
        return !!(body.id || body.Id);
      } catch {
        return false;
      }
    },
    'POST /api/rooms duration < 300ms': r => r.timings.duration < 300,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'rooms-create', 'rooms_create');
}