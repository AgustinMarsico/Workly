export function testEnvironment(overrides = {}) {
  const env = {
    ...process.env,
    EMAIL_DELIVERY_MODE: 'log',
    SQS_QUEUE_URL: 'http://127.0.0.1:1/reservations',
    SQS_INVITATION_QUEUE_URL: 'http://127.0.0.1:1/invitations',
    INVITATION_BASE_URL: 'http://localhost:8080/invitations/accept',
    AWS_REGION: 'us-east-1',
    AWS_ACCESS_KEY_ID: 'test',
    AWS_SECRET_ACCESS_KEY: 'test',
    AWS_SESSION_TOKEN: '',
    AWS_EC2_METADATA_DISABLED: 'true',
    EMAIL_SMTP_USERNAME: '',
    EMAIL_SMTP_PASSWORD: '',
    NEW_RELIC_ENABLED: 'false',
    NEW_RELIC_LICENSE_KEY: '',
    BREVO_API_KEY: 'key-for-local-adapter-tests',
    BREVO_SENDER_EMAIL: 'sender@example.com',
    ...overrides,
  };
  return env;
}

export function emailSettings(mode) {
  return {
    brevo: {
      deliveryMode: mode,
      apiKey: 'key-for-local-adapter-tests',
      senderEmail: 'sender@example.com',
      senderName: 'Workly',
    },
    smtp: {
      host: '127.0.0.1',
      port: 1025,
      useSsl: false,
      username: '',
      password: '',
      senderEmail: 'sender@example.com',
      senderName: 'Workly',
    },
  };
}

export function invitation(overrides = {}) {
  return {
    InvitationId: 'invitation-test',
    UserEmail: 'member@example.com',
    CompanyId: 'company-test',
    CompanyName: 'Demo Company',
    AreaName: 'Engineering',
    PlainToken: 'local-only-invitation-token',
    ...overrides,
  };
}

export function reservation(overrides = {}) {
  return {
    emails: ['member@example.com'],
    emailSubscribed: true,
    action: 'created',
    actionAt: '2026-11-20T10:00:00Z',
    reservation: {
      id: 'reservation-test',
      space: { name: 'Demo Room' },
      startAt: '2026-11-21T10:00:00Z',
      endAt: '2026-11-21T11:00:00Z',
    },
    user: { id: 'user-test', name: 'Demo User', email: 'member@example.com' },
    ...overrides,
  };
}

export function capturingLogger() {
  const entries = [];
  return {
    entries,
    log: (...args) => entries.push(args),
    error: (...args) => entries.push(args),
  };
}
