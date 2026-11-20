import http from 'k6/http';
import { check, fail } from 'k6';
import { endpoints, buildUrl } from '../config/endpoints.js';

export function loginMember(baseUrl) {
  const email = __ENV.MEMBER_EMAIL || 'usuario@saas.com';
  const password = __ENV.MEMBER_PASSWORD || 'Password1';
  const url = buildUrl(baseUrl, endpoints.login);

  const res = http.post(url, JSON.stringify({ email, password }), {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'login_member' },
  });

  const ok = check(res, {
    'login member status 200': r => r.status === 200,
    'login member returns token': r => !!r.json('token'),
  });

  if (!ok) {
    fail(`No se pudo hacer login como member. Status: ${res.status}. Body: ${res.body}`);
  }

  return res.json('token');
}
