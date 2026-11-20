import http from 'node:http';
import { config } from './config.js';
import { parseSnsMessage, processMessage } from './processor.js';
import { deleteMessage, getQueueDepth, receiveMessages } from './sqs/client.js';

let isRunning = true;
let newrelic = {
  recordLogEvent() {},
  incrementMetric() {},
  recordMetric() {},
  noticeError() {},
};

if (process.env.NEW_RELIC_LICENSE_KEY) {
  try {
    newrelic = (await import('newrelic')).default;
  } catch (error) {
    console.warn(`New Relic disabled: ${error.message}`);
  }
}

const queues = [
  { name: 'reservation', url: config.aws.reservationQueueUrl },
  { name: 'invitation', url: config.aws.invitationQueueUrl },
];

function log(level, message, meta = {}) {
  const entry = {
    timestamp: new Date().toISOString(),
    service: process.env.NEW_RELIC_APP_NAME || 'notification-worker',
    level,
    message,
    ...meta,
  };
  console.log(JSON.stringify(entry));
  newrelic.recordLogEvent({
    timestamp: Date.now(),
    level,
    message,
    ...meta,
  });
}

async function handleMessage(queueName, queueUrl, message) {
  const messageId = message.MessageId;
  const correlationId =
    message.MessageAttributes?.['X-Correlation-ID']?.StringValue ??
    undefined;

  try {
    const payload = parseSnsMessage(message.Body);
    const payloadCorrelationId =
      payload.correlationId ?? payload.CorrelationId ?? correlationId;
    log('info', 'Processing message', {
      messageId,
      queue: queueName,
      status: 'pending',
      correlationId: payloadCorrelationId,
      emails: payload.emails ?? payload.Emails,
      email:
        payload.email ??
        payload.to ??
        payload.user?.email ??
        payload.UserEmail ??
        payload.userEmail,
      reservationId: payload.reservation?.id,
      invitationId: payload.InvitationId ?? payload.invitationId,
      action: payload.action,
    });

    const result = await processMessage(payload);
    await deleteMessage(queueUrl, message.ReceiptHandle);
    newrelic.incrementMetric(`Custom/SQS/${queueName}/Processed`);

    log('info', 'Message processed and deleted', {
      messageId,
      queue: queueName,
      status: 'processed',
      correlationId: payloadCorrelationId,
      result,
    });
  } catch (error) {
    newrelic.incrementMetric(`Custom/SQS/${queueName}/Failed`);
    newrelic.noticeError(error);
    log('error', 'Failed to process message', {
      messageId,
      queue: queueName,
      status: 'failed',
      correlationId,
      error: error.message,
    });
  }
}

async function poll() {
  while (isRunning) {
    try {
      const batches = await Promise.all(
        queues.map(async ({ name, url }) => {
          const depth = await getQueueDepth(url);
          newrelic.recordMetric(`Custom/SQS/${name}/PendingMessages`, depth.pending);
          newrelic.recordMetric(`Custom/SQS/${name}/InFlightMessages`, depth.inFlight);
          log('info', 'Queue depth observed', {
            queue: name,
            pendingMessages: depth.pending,
            inFlightMessages: depth.inFlight,
          });

          const messages = await receiveMessages(url);
          return messages.map((message) => ({ name, url, message }));
        }),
      );

      const pending = batches.flat();

      if (pending.length === 0) {
        await sleep(config.pollIntervalMs);
        continue;
      }

      await Promise.all(
        pending.map(({ name, url, message }) =>
          handleMessage(name, url, message),
        ),
      );
    } catch (error) {
      log('error', 'Polling cycle failed', { error: error.message });
      await sleep(config.pollIntervalMs);
    }
  }
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function shutdown(signal) {
  log('info', `Received ${signal}, shutting down worker...`);
  isRunning = false;
  healthServer.close();
}

process.on('SIGINT', () => shutdown('SIGINT'));
process.on('SIGTERM', () => shutdown('SIGTERM'));

log('info', 'Starting notification worker', {
  region: config.aws.region,
  reservationQueueUrl: config.aws.reservationQueueUrl,
  invitationQueueUrl: config.aws.invitationQueueUrl,
});

const healthServer = http.createServer((request, response) => {
  if (request.url === '/health') {
    response.writeHead(200, { 'content-type': 'application/json' });
    response.end(JSON.stringify({ status: 'healthy', service: 'notification-worker' }));
    return;
  }

  response.writeHead(404, { 'content-type': 'application/json' });
  response.end(JSON.stringify({ status: 'not_found' }));
});

const healthReady = new Promise((resolve, reject) => {
  healthServer.once('listening', resolve);
  healthServer.once('error', reject);
});

healthServer.listen(config.healthPort, () => {
  log('info', 'Health endpoint listening', { port: config.healthPort });
});

await healthReady;
await poll();
