import http from 'k6/http';
import { check, fail } from 'k6';
import { endpoints, buildUrl } from '../config/endpoints.js';

export function createApiKey(baseUrl, memberToken, name = `k6-api-key-${Date.now()}`) {
  const res = http.post(
    buildUrl(baseUrl, endpoints.apiKeys),
    JSON.stringify({ name }),
    {
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${memberToken}`,
      },
      tags: {
        endpoint: 'setup_api_key_create',
      },
    }
  );

  const ok = check(res, {
    'setup POST /api/api-keys status 201': r => r.status === 201,
  });

  if (!ok || !res.body) {
    fail(`No se pudo crear API key. Status: ${res.status}. Body: ${res.body}`);
  }

  const body = res.json();

  const key =
    body.key ||
    body.value ||
    body.apiKey ||
    body.token ||
    body.secret ||
    body.plainTextKey ||
    body.PlainTextKey ||
    body.Key ||
    body.Value ||
    body.ApiKey;

  if (!key) {
    fail(`No se encontró el valor de la API key en la respuesta. Body: ${res.body}`);
  }

  return key;
}

export function apiKeyHeaders(apiKey) {
  const headerName = __ENV.API_KEY_HEADER_NAME || 'X-API-Key';
  const authScheme = __ENV.API_KEY_AUTH_SCHEME || '';

  if (headerName.toLowerCase() === 'authorization') {
    return {
      Authorization: authScheme ? `${authScheme} ${apiKey}` : apiKey,
    };
  }

  return {
    [headerName]: apiKey,
  };
}