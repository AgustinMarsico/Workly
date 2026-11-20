import http from 'k6/http';
import { check, fail } from 'k6';
import { endpoints, buildUrl } from '../config/endpoints.js';

export function getOrCreateRoom(mainServiceUrl, token) {
  if (__ENV.RESERVATION_ROOM_ID) {
    return __ENV.RESERVATION_ROOM_ID;
  }

  const existingRoomId = tryGetExistingRoom(mainServiceUrl, token);
  if (existingRoomId) {
    return existingRoomId;
  }

  const unique = `${__VU}-${__ITER}-${Date.now()}`;
  const payload = {
    name: `Sala K6 Reservation Service ${unique}`,
    typeKey: __ENV.ROOM_TYPE || 'Sala',
    maxCapacity: Number(__ENV.ROOM_CAPACITY || 8),
    equipment: __ENV.ROOM_EQUIPMENT || 'TV, pizarra, videoconferencia',
  };

  const res = http.post(buildUrl(mainServiceUrl, endpoints.rooms), JSON.stringify(payload), {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    tags: { endpoint: 'setup_rooms_create' },
  });

  const ok = check(res, {
    'setup room status 201': r => r.status === 201,
    'setup room returns id': r => !!(r.json('id') || r.json('Id')),
  });

  if (!ok) {
    fail(`No se pudo crear room. Status: ${res.status}. Body: ${res.body}`);
  }

  return res.json('id') || res.json('Id');
}

function tryGetExistingRoom(mainServiceUrl, token) {
  const res = http.get(buildUrl(mainServiceUrl, endpoints.rooms), {
    headers: { Authorization: `Bearer ${token}` },
    tags: { endpoint: 'setup_rooms_get_all' },
  });

  if (res.status !== 200 || !res.body) {
    return null;
  }

  const rooms = res.json();
  if (!Array.isArray(rooms) || rooms.length === 0) {
    return null;
  }

  return rooms[0].id || rooms[0].Id || null;
}
