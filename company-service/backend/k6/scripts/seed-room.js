import http from 'k6/http';
import { check, fail } from 'k6';
import { endpoints, buildUrl } from '../config/endpoints.js';
import { loginMember } from '../lib/auth.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

const ROOM_NAME = __ENV.ROOM_NAME || `Sala K6 Demo ${Date.now()}`;
const ROOM_TYPE = __ENV.ROOM_TYPE || 'Sala';
const ROOM_CAPACITY = Number(__ENV.ROOM_CAPACITY || 8);
const ROOM_EQUIPMENT = __ENV.ROOM_EQUIPMENT || 'TV, pizarra, videoconferencia';

export const options = {
  scenarios: {
    seed_room: {
      executor: 'shared-iterations',
      vus: 1,
      iterations: 1,
    },
  },
};

export default function () {
  const token = loginMember(BASE_URL);

  const payload = {
    name: ROOM_NAME,
    typeKey: ROOM_TYPE,
    maxCapacity: ROOM_CAPACITY,
    equipment: ROOM_EQUIPMENT,
  };

  console.log(`payload=${JSON.stringify(payload)}`);

  const url = buildUrl(BASE_URL, endpoints.rooms);

  const res = http.post(url, JSON.stringify(payload), {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
  });

  check(res, {
    'POST /api/rooms status 200 or 201': r => r.status === 200 || r.status === 201,
    'POST /api/rooms returns body': r => !!r.body,
  });

  if (res.status !== 200 && res.status !== 201) {
    fail(`No se pudo crear la room. Status: ${res.status}. Body: ${res.body}`);
  }

  const body = res.json();
  const roomId = body.id || body.Id;

  if (!roomId) {
    fail(`La respuesta no contiene id. Body: ${res.body}`);
  }

  console.log('');
  console.log('======================================');
  console.log('Room creada correctamente');
  console.log(`RESERVATION_ROOM_ID=${roomId}`);
  console.log('Copiá esa línea en k6/.env');
  console.log('======================================');
}
