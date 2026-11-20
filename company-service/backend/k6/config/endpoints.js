export const endpoints = {
  // Auth
  login: '/api/auth/login',

  // Health
  health: '/health',

  // Users
  users: '/api/users',
  userById: '/api/users/:id',

  // Companies
  companies: '/api/companies',
  companyById: '/api/companies/:id',

  // Rooms
  roomTypes: '/api/rooms/types',
  rooms: '/api/rooms',
  roomById: '/api/rooms/:id',
  topRooms: '/api/rooms/top',

  // Invitations
  invitations: '/api/invitations',
  invitationsAccept: '/api/invitations/accept',
  invitationById: '/api/invitations/:id',

  // API Keys
  apiKeys: '/api/api-keys',
  apiKeyById: '/api/api-keys/:id',
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

  return queryString
    ? `${baseUrl}${path}?${queryString}`
    : `${baseUrl}${path}`;
}
