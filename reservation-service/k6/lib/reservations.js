import http from 'k6/http';
import { check } from 'k6';
import exec from 'k6/execution';
import { endpoints, buildUrl } from '../config/endpoints.js';

const RUN_DAY_OFFSET = Number(
  __ENV.RESERVATION_RUN_DAY_OFFSET ||
  (Math.floor(Date.now() / 1000) % 100000)
);

const BASE_START_MS =
  Date.UTC(2035, 0, 1, 8, 0, 0) + RUN_DAY_OFFSET * 24 * 60 * 60 * 1000;

export function reservationTimeSlot(durationMinutes = 30) {
  const iteration = exec?.scenario?.iterationInTest !== undefined
    ? exec.scenario.iterationInTest
    : (__ITER * 1000 + __VU);

  const startMs = BASE_START_MS + iteration * 60 * 60 * 1000;
  const endMs = startMs + durationMinutes * 60 * 1000;

  return {
    start: new Date(startMs).toISOString(),
    end: new Date(endMs).toISOString(),
  };
}

export function createReservationForTest(reservationServiceUrl, token, roomId, tag = 'setup_reservation_create') {
  const { start, end } = reservationTimeSlot();
  const payload = { roomId, start, end };

  const res = http.post(buildUrl(reservationServiceUrl, endpoints.reservations), JSON.stringify(payload), {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    tags: { endpoint: tag },
  });

  const ok = check(res, {
    [`${tag} status 201`]: r => r.status === 201,
    [`${tag} returns id`]: r => !!(r.json('id') || r.json('Id')),
  });

  if (!ok) {
    throw new Error(`No se pudo crear reserva auxiliar. Status: ${res.status}. Body: ${res.body}`);
  }

  return res.json('id') || res.json('Id');
}
