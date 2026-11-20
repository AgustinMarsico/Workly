import http from 'k6/http';
import { check, fail } from 'k6';
import { endpoints, buildUrl } from '../config/endpoints.js';

export function login(baseUrl, email, password, roleName = 'user') {
  const url = buildUrl(baseUrl, endpoints.login);

  const res = http.post(
    url,
    JSON.stringify({
      email,
      password,
    }),
    {
      headers: {
        'Content-Type': 'application/json',
      },
      tags: {
        endpoint: `login_${roleName}`,
      },
    }
  );

  const ok = check(res, {
    [`login ${roleName} status 200`]: r => r.status === 200,
    [`login ${roleName} returns token`]: r => {
      if (r.status !== 200 || !r.body) {
        return false;
      }

      try {
        return !!r.json('token');
      } catch {
        return false;
      }
    },
  });

  if (!ok) {
    fail(`No se pudo hacer login como ${roleName}. Status: ${res.status}. Body: ${res.body}`);
  }

  return res.json('token');
}

export function loginAdmin(baseUrl) {
  const email = __ENV.ADMIN_EMAIL || 'admin@saas.com';
  const password = __ENV.ADMIN_PASSWORD || 'Password1';

  return login(baseUrl, email, password, 'admin');
}

export function loginMember(baseUrl) {
  const email = __ENV.MEMBER_EMAIL || 'usuario@saas.com';
  const password = __ENV.MEMBER_PASSWORD || 'Password1';

  return login(baseUrl, email, password, 'member');
}