const fs = require('fs');
const path = require('path');

function parseDotEnv(content) {
  const lines = content.split(/\r?\n/);
  const env = {};
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#')) continue;
    const idx = trimmed.indexOf('=');
    if (idx === -1) continue;
    const key = trimmed.substring(0, idx).trim();
    let val = trimmed.substring(idx + 1).trim();
    if (val.startsWith('"') && val.endsWith('"')) val = val.slice(1, -1);
    if (val.startsWith("'") && val.endsWith("'")) val = val.slice(1, -1);
    env[key] = val;
  }
  return env;
}

const projectRoot = path.resolve(__dirname, '..');
const envPath = path.join(projectRoot, '.env');
let env = {};
if (fs.existsSync(envPath)) {
  const content = fs.readFileSync(envPath, 'utf8');
  env = parseDotEnv(content);
}

function resolveApiUrl() {
  // Prefer the build environment first so Amplify can inject the production URL.
  if (process.env.API_URL) return process.env.API_URL;
  if (env.API_URL) return env.API_URL;
  return 'http://localhost:5000';
}

function resolveReservationApiUrl(apiUrl) {
  if (process.env.RESERVATION_API_URL) return process.env.RESERVATION_API_URL;
  if (env.RESERVATION_API_URL) return env.RESERVATION_API_URL;
  return 'http://localhost:5200';
}

function resolveIdentityApiUrl(apiUrl) {
  if (process.env.IDENTITY_API_URL) return process.env.IDENTITY_API_URL;
  if (env.IDENTITY_API_URL) return env.IDENTITY_API_URL;
  return 'http://localhost:5100';
}

function resolveRoomsApiUrl(apiUrl) {
  if (process.env.ROOMS_API_URL) return process.env.ROOMS_API_URL;
  if (env.ROOMS_API_URL) return env.ROOMS_API_URL;
  return 'http://localhost:5300';
}

function resolveAuditApiUrl(apiUrl) {
  if (process.env.AUDIT_API_URL) return process.env.AUDIT_API_URL;
  if (env.AUDIT_API_URL) return env.AUDIT_API_URL;
  return 'http://localhost:5151';
}

const apiUrl = resolveApiUrl();
const reservationApiUrl = resolveReservationApiUrl(apiUrl);
const identityApiUrl = resolveIdentityApiUrl(apiUrl);
const roomsApiUrl = resolveRoomsApiUrl(apiUrl);
const auditApiUrl = resolveAuditApiUrl(apiUrl);
const fileContent = `(function (global) {
  global.__WORKLY_API__ = global.__WORKLY_API__ || {};
  global.__WORKLY_API__.baseUrl = '${apiUrl}';
  global.__WORKLY_API__.identityBaseUrl = '${identityApiUrl}';
  global.__WORKLY_API__.reservationBaseUrl = '${reservationApiUrl}';
  global.__WORKLY_API__.roomsBaseUrl = '${roomsApiUrl}';
  global.__WORKLY_API__.auditBaseUrl = '${auditApiUrl}';
})(typeof window !== 'undefined' ? window : globalThis);
`;

const outputPaths = [
  path.join(projectRoot, 'public', 'env.js'),
  path.join(projectRoot, 'src', 'assets', 'env.js'),
];

for (const outPath of outputPaths) {
  const outDir = path.dirname(outPath);
  if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, { recursive: true });
  fs.writeFileSync(outPath, fileContent, 'utf8');
  console.log('Wrote', outPath, '->', apiUrl, identityApiUrl, reservationApiUrl, roomsApiUrl, auditApiUrl);
}
