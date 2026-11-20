import http from 'k6/http';
import { check } from 'k6';

export const options = {
  scenarios: {
    top_rooms_load: {
      executor: 'constant-arrival-rate',
      rate: 20,          // 20 req/s = 1200 req/min
      timeUnit: '1s',
      duration: '2m',
      preAllocatedVUs: 20,
      maxVUs: 100,
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<300'],
    http_req_failed: ['rate<0.01'],
    checks: ['rate>0.99'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const LOGIN_EMAIL = __ENV.LOGIN_EMAIL || 'usuario@saas.com';
const LOGIN_PASSWORD = __ENV.LOGIN_PASSWORD || 'Password1';

export function setup() {
  // 1) Login
  const loginPayload = JSON.stringify({
    email: LOGIN_EMAIL,
    password: LOGIN_PASSWORD,
  });

  const loginRes = http.post(`${BASE_URL}/api/auth/login`, loginPayload, {
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    },
  });

  check(loginRes, {
    'login status 200': (r) => r.status === 200,
    'login devuelve token': (r) => {
      try {
        const body = r.json();
        return !!body.token;
      } catch {
        return false;
      }
    },
  });

  if (loginRes.status !== 200) {
    throw new Error(`Login falló. Status: ${loginRes.status}. Body: ${loginRes.body}`);
  }

  const loginBody = loginRes.json();
  const jwt = loginBody.token;

  // 2) Crear API key
  // Uso un nombre único para evitar colisiones si corrés varias veces.
  const apiKeyName = `k6-key-${Date.now()}`;

  const createKeyPayload = JSON.stringify({
    name: apiKeyName,
  });

  const createKeyRes = http.post(`${BASE_URL}/api/api-keys`, createKeyPayload, {
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      'Authorization': `Bearer ${jwt}`,
    },
  });

  check(createKeyRes, {
    'create api key status 200/201': (r) => r.status === 200 || r.status === 201,
    'create api key devuelve apiKey': (r) => {
      try {
        const body = r.json();
        return !!body.apiKey;
      } catch {
        return false;
      }
    },
  });

  if (!(createKeyRes.status === 200 || createKeyRes.status === 201)) {
    throw new Error(`Crear API key falló. Status: ${createKeyRes.status}. Body: ${createKeyRes.body}`);
  }

  const keyBody = createKeyRes.json();
  const apiKey = keyBody.apiKey;

  return {
    apiKey,
  };
}

export default function (data) {
  const res = http.get(`${BASE_URL}/api/rooms/top`, {
    headers: {
      'X-API-Key': data.apiKey,
      'Accept': 'application/json',
    },
  });

  check(res, {
    'top rooms status 200': (r) => r.status === 200,
    'top rooms devuelve array': (r) => {
      try {
        return Array.isArray(r.json());
      } catch {
        return false;
      }
    },
    'top rooms max 3 elementos': (r) => {
      try {
        const body = r.json();
        return Array.isArray(body) && body.length <= 3;
      } catch {
        return false;
      }
    },
  });
}