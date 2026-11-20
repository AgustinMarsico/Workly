import http from "k6/http";
import { check, fail } from "k6";

export const options = {
  scenarios: {
    create_users_1200_per_minute: {
      executor: "constant-arrival-rate",
      rate: 20, // 20 req/s = 1200 req/min
      timeUnit: "1s",
      duration: "1m",
      preAllocatedVUs: 50,
      maxVUs: 200,
    },
  },
  thresholds: {
    http_req_duration: ["p(95)<300"],
    http_req_failed: ["rate<0.01"],
  },
};

const BASE_URL = "http://localhost:5000";

export function setup() {
  const loginPayload = JSON.stringify({
    email: "admin@saas.com",
    password: "Password1",
  });

  const loginRes = http.post(`${BASE_URL}/api/auth/login`, loginPayload, {
    headers: {
      "Content-Type": "application/json",
    },
  });

  const loginOk = check(loginRes, {
    "login status is 200": (r) => r.status === 200,
    "login has token": (r) => Boolean(r.json("token")),
  });

  if (!loginOk) {
    fail(`Login failed. Status: ${loginRes.status}. Body: ${loginRes.body}`);
  }

  return {
    token: loginRes.json("token"),
  };
}

export default function (data) {
  const unique = `${__VU}-${__ITER}-${Date.now()}`;

  const payload = JSON.stringify({
    email: `user-${unique}@test.com`,
    password: "Hola",
    role: "Admin",
  });

  const params = {
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${data.token}`,
    },
  };

  const res = http.post(`${BASE_URL}/api/users`, payload, params);

  check(res, {
    "create user status is 200 or 201": (r) => r.status === 200 || r.status === 201,
    "create user p/request < 300ms": (r) => r.timings.duration < 300,
  });
}