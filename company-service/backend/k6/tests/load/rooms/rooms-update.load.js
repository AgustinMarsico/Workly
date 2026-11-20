import http from 'k6/http';
import { check } from 'k6';
import { endpoints, buildUrl } from '../../../config/endpoints.js';
import { loginMember } from '../../../lib/auth.js';
import { createRoomForTest } from '../../../lib/rooms.js';
import { buildSummary } from '../../../lib/summary.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TEST_DURATION = __ENV.TEST_DURATION || '1m';
const RATE = Number(__ENV.RATE || 20);

export const options = {
  scenarios: {
    rooms_update_load_test: {
      executor: 'constant-arrival-rate',
      rate: RATE,
      timeUnit: '1s',
      duration: TEST_DURATION,
      preAllocatedVUs: 50,
      maxVUs: 300,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:rooms_update}': ['p(95)<300'],
    'http_req_failed{endpoint:rooms_update}': ['rate<0.01'],
    'http_reqs{endpoint:rooms_update}': ['count>0'],
  },
};

export function setup() {
  return {
    token: loginMember(BASE_URL),
  };
}

export default function (data) {
  const created = createRoomForTest(
    BASE_URL,
    data.token,
    {},
    'setup_rooms_create_for_update'
  );

  const unique = `${__VU}-${__ITER}-${Date.now()}`;

  const payload = {
    name: `Sala K6 Updated ${unique}`,
    typeKey: 'Sala',
    maxCapacity: 10,
    equipment: 'TV, pizarra, videoconferencia, HDMI',
  };

  const url = buildUrl(BASE_URL, endpoints.roomById, {
    id: created.id,
  });

  const res = http.put(url, JSON.stringify(payload), {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${data.token}`,
    },
    tags: {
      endpoint: 'rooms_update',
    },
  });

  check(res, {
    'PUT /api/rooms/{id} status 200': r => r.status === 200,
    'PUT /api/rooms/{id} duration < 300ms': r => r.timings.duration < 300,
  });
}

export function handleSummary(data) {
  return buildSummary(data, 'rooms-update', 'rooms_update');
}