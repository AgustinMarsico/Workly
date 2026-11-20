export const APP_ENV = {
  apiBaseUrl: (globalThis as any).__WORKLY_API__?.baseUrl ?? 'http://localhost:5000',
  identityApiBaseUrl:
    (globalThis as any).__WORKLY_API__?.identityBaseUrl ??
    (globalThis as any).__WORKLY_API__?.baseUrl ??
    'http://localhost:5100',
  reservationApiBaseUrl:
    (globalThis as any).__WORKLY_API__?.reservationBaseUrl ??
    (globalThis as any).__WORKLY_API__?.baseUrl ??
    'http://localhost:5000',
  roomsApiBaseUrl:
    (globalThis as any).__WORKLY_API__?.roomsBaseUrl ??
    (globalThis as any).__WORKLY_API__?.baseUrl ??
    'http://localhost:5300',
  auditApiBaseUrl:
    (globalThis as any).__WORKLY_API__?.auditBaseUrl ??
    (globalThis as any).__WORKLY_API__?.baseUrl ??
    'http://localhost:5151',
};
