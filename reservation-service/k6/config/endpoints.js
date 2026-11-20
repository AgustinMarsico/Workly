export const endpoints = {
  login: '/api/auth/login',
  rooms: '/api/rooms',
  reservations: '/api/reservations',
  reservationById: '/api/reservations/:id',
  upcomingReservations: '/api/reservations/upcoming',
  upcomingReservationStats: '/api/reservations/upcoming/stats',
  reservationHistory: '/api/reservations/history',
  reservationSearch: '/api/reservations/search',
  apiKeys: '/api/api-keys',
};

export function buildUrl(baseUrl, endpoint, params = {}, query = {}) {
  let path = endpoint;

  for (const [key, value] of Object.entries(params)) {
    path = path.replace(`:${key}`, encodeURIComponent(value));
  }

  const queryString = Object.entries(query)
    .filter(([, value]) => value !== undefined && value !== null && value !== '')
    .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)
    .join('&');

  return queryString ? `${baseUrl}${path}?${queryString}` : `${baseUrl}${path}`;
}
