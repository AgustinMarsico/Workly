import http from 'k6/http';
import { check, fail } from 'k6';
import { endpoints, buildUrl } from '../config/endpoints.js';

const ROOM_TYPE = __ENV.ROOM_TYPE || 'Sala';
const ROOM_CAPACITY = Number(__ENV.ROOM_CAPACITY || 8);
const ROOM_EQUIPMENT = __ENV.ROOM_EQUIPMENT || 'TV, pizarra, videoconferencia';

export function getOrCreateRoom(baseUrl, memberToken) {
  if (__ENV.RESERVATION_ROOM_ID) {
    return __ENV.RESERVATION_ROOM_ID;
  }

  const existingRoomId = tryGetExistingRoom(baseUrl, memberToken);

  if (existingRoomId) {
    return existingRoomId;
  }

  const created = createRoomForTest(baseUrl, memberToken, {}, 'setup_rooms_create');
  return created.id;
}

export function tryGetExistingRoom(baseUrl, memberToken) {
  const url = buildUrl(baseUrl, endpoints.rooms);

  const res = http.get(url, {
    headers: {
      Authorization: `Bearer ${memberToken}`,
    },
    tags: {
      endpoint: 'setup_rooms_get_all',
    },
  });

  if (res.status !== 200 || !res.body) {
    return null;
  }

  try {
    const rooms = res.json();

    if (!Array.isArray(rooms) || rooms.length === 0) {
      return null;
    }

    const room = rooms[0];
    return room.id || room.Id || null;
  } catch {
    return null;
  }
}

export function createRoomForTest(baseUrl, memberToken, overrides = {}, tag = 'setup_rooms_create') {
  const vu = typeof __VU !== 'undefined' ? __VU : 0;
  const iter = typeof __ITER !== 'undefined' ? __ITER : 0;
  const unique = `${vu}-${iter}-${Date.now()}`;

  const payload = {
    name: overrides.name || `Sala K6 ${unique}`,
    typeKey: overrides.typeKey || ROOM_TYPE,
    maxCapacity: overrides.maxCapacity || ROOM_CAPACITY,
    equipment: overrides.equipment ?? ROOM_EQUIPMENT,
  };

  const url = buildUrl(baseUrl, endpoints.rooms);

  const res = http.post(url, JSON.stringify(payload), {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${memberToken}`,
    },
    tags: {
      endpoint: tag,
    },
  });

  const ok = check(res, {
    [`${tag} status 201`]: r => r.status === 201,
    [`${tag} returns id`]: r => {
      if (r.status !== 201 || !r.body) return false;

      try {
        const body = r.json();
        return !!(body.id || body.Id);
      } catch {
        return false;
      }
    },
  });

  if (!ok) {
    fail(`No se pudo crear room. Status: ${res.status}. Body: ${res.body}`);
  }

  const body = res.json();

  return {
    id: body.id || body.Id,
    body,
  };
}