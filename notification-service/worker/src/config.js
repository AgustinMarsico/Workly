import { config as loadDotEnv } from 'dotenv';
import { existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const sourceDir = dirname(fileURLToPath(import.meta.url));
const envCandidates = [
  join(sourceDir, '..', '..', '.env'),
  join(sourceDir, '..', '.env'),
  join(process.cwd(), '.env'),
  join(process.cwd(), '..', '.env'),
];

for (const envPath of envCandidates) {
  if (existsSync(envPath)) {
    loadDotEnv({ path: envPath });
    break;
  }
}

const required = (name) => {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Required environment variable: ${name}`);
  }
  return value;
};

const emailDeliveryMode = process.env.EMAIL_DELIVERY_MODE ?? 'brevo';
if (!['brevo', 'smtp', 'log'].includes(emailDeliveryMode)) {
  throw new Error('EMAIL_DELIVERY_MODE must be brevo, smtp, or log');
}

const smtpUsername = process.env.EMAIL_SMTP_USERNAME ?? '';
const smtpPassword = process.env.EMAIL_SMTP_PASSWORD ?? '';
if (emailDeliveryMode === 'smtp' && Boolean(smtpUsername) !== Boolean(smtpPassword)) {
  throw new Error('Set both EMAIL_SMTP_USERNAME and EMAIL_SMTP_PASSWORD, or neither');
}
const requiredForMode = (mode, name) => {
  if (emailDeliveryMode !== mode) {
    return process.env[name] ?? '';
  }

  return required(name);
};

export const config = {
  aws: {
    region: process.env.AWS_REGION ?? 'us-east-1',
    reservationQueueUrl: required('SQS_QUEUE_URL'),
    invitationQueueUrl: required('SQS_INVITATION_QUEUE_URL'),
    endpoint: process.env.AWS_ENDPOINT_URL,
    waitTimeSeconds: Number(process.env.SQS_WAIT_TIME_SECONDS ?? 20),
    maxMessages: Number(process.env.SQS_MAX_MESSAGES ?? 10),
    visibilityTimeout: Number(process.env.SQS_VISIBILITY_TIMEOUT ?? 60),
  },
  invitation: {
    baseUrl: required('INVITATION_BASE_URL'),
  },
  brevo: {
    deliveryMode: emailDeliveryMode,
    apiKey: requiredForMode('brevo', 'BREVO_API_KEY'),
    senderEmail: requiredForMode('brevo', 'BREVO_SENDER_EMAIL'),
    senderName: process.env.BREVO_SENDER_NAME ?? 'Notifications',
  },
  smtp: {
    host: requiredForMode('smtp', 'EMAIL_SMTP_HOST'),
    port: Number(process.env.EMAIL_SMTP_PORT ?? 587),
    useSsl: (process.env.EMAIL_SMTP_USE_SSL ?? 'true').toLowerCase() === 'true',
    username: smtpUsername,
    password: smtpPassword,
    senderEmail:
      process.env.EMAIL_FROM ??
      process.env.BREVO_SENDER_EMAIL ??
      'noreply@workly.local',
    senderName: process.env.BREVO_SENDER_NAME ?? 'Workly',
  },
  pollIntervalMs: Number(process.env.POLL_INTERVAL_MS ?? 1000),
  healthPort: Number(process.env.HEALTH_PORT ?? 8080),
};
