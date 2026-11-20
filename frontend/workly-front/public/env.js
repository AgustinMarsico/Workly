(function (global) {
  global.__WORKLY_API__ = global.__WORKLY_API__ || {};
  global.__WORKLY_API__.baseUrl = 'http://localhost:5000';
  global.__WORKLY_API__.identityBaseUrl = 'http://localhost:5100';
  global.__WORKLY_API__.reservationBaseUrl = 'http://localhost:5200';
  global.__WORKLY_API__.roomsBaseUrl = 'http://localhost:5300';
  global.__WORKLY_API__.auditBaseUrl = 'http://localhost:5151';
})(typeof window !== 'undefined' ? window : globalThis);
