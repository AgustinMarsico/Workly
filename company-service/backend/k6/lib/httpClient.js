export function bearerHeaders(token) {
  return {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
  };
}

export function apiKeyHeaders(apiKey) {
  return {
    headers: {
      'Content-Type': 'application/json',
      'X-API-KEY': apiKey,
    },
  };
}