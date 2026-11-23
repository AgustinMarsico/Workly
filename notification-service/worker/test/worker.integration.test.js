import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { once } from 'node:events';
import http from 'node:http';
import net from 'node:net';
import { spawn, spawnSync } from 'node:child_process';
import { setTimeout as delay } from 'node:timers/promises';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';
import { invitation, reservation, testEnvironment } from '../support/fixtures.js';

const workerRoot = fileURLToPath(new URL('..', import.meta.url));

async function listen(server) {
  server.listen(0, '127.0.0.1');
  await once(server, 'listening');
  return server.address().port;
}

test('configuration rejects invalid modes and partial SMTP credentials', () => {
  for (const overrides of [
    { EMAIL_DELIVERY_MODE: 'unknown' },
    { EMAIL_DELIVERY_MODE: 'smtp', EMAIL_SMTP_HOST: '127.0.0.1', EMAIL_SMTP_USERNAME: 'user', EMAIL_SMTP_PASSWORD: '' },
  ]) {
    const result = spawnSync(process.execPath, ['--input-type=module', '-e', "await import('./src/config.js')"], {
      cwd: workerRoot,
      env: testEnvironment(overrides),
      encoding: 'utf8',
    });
    assert.notEqual(result.status, 0);
    assert.match(result.stderr, /EMAIL_DELIVERY_MODE|Set both EMAIL_SMTP_USERNAME/);
  }
});

test('worker consumes both queues, delivers local SMTP and does not acknowledge failures', { timeout: 20000 }, async (context) => {
  const messages = [];
  const smtpSockets = new Set();
  const smtp = net.createServer((socket) => {
    smtpSockets.add(socket);
    socket.on('close', () => smtpSockets.delete(socket));
    socket.on('error', () => {});
    socket.setEncoding('utf8');
    socket.write('220 local-test SMTP\r\n');
    let buffer = '';
    let dataMode = false;
    let recipientAccepted = false;
    let lines = [];
    socket.on('data', (chunk) => {
      buffer += chunk;
      let boundary;
      while ((boundary = buffer.indexOf('\r\n')) !== -1) {
        const line = buffer.slice(0, boundary);
        buffer = buffer.slice(boundary + 2);
        if (dataMode) {
          if (line === '.') {
            messages.push(lines.join('\r\n'));
            lines = [];
            dataMode = false;
            socket.write('250 queued locally\r\n');
          } else {
            lines.push(line);
          }
        } else if (/^(EHLO|HELO)/i.test(line)) {
          socket.write('250-local-test\r\n250 PIPELINING\r\n');
        } else if (/^(MAIL FROM:|RSET)/i.test(line)) {
          recipientAccepted = false;
          socket.write('250 OK\r\n');
        } else if (/^RCPT TO:.*reject@example\.com/i.test(line)) {
          socket.write('550 Recipient rejected for test\r\n');
        } else if (/^RCPT TO:/i.test(line)) {
          recipientAccepted = true;
          socket.write('250 OK\r\n');
        } else if (/^DATA$/i.test(line)) {
          if (recipientAccepted) {
            dataMode = true;
            socket.write('354 End with a dot\r\n');
          } else {
            socket.write('554 No accepted recipients\r\n');
          }
        } else if (/^QUIT$/i.test(line)) {
          socket.end('221 goodbye\r\n');
        } else {
          socket.write('250 OK\r\n');
        }
      }
    });
  });
  context.after(() => {
    for (const socket of smtpSockets) socket.destroy();
    smtp.close();
  });
  const smtpPort = await listen(smtp);

  const delivered = new Set();
  const deleted = new Set();
  let failedMessageObserved = false;
  let complete;
  const completed = new Promise((resolve) => { complete = resolve; });
  const maybeComplete = () => {
    if (deleted.size === 2 && failedMessageObserved) complete();
  };
  const sqsMessage = (payload, receipt) => {
    const body = JSON.stringify({ Type: 'Notification', Message: JSON.stringify(payload) });
    return {
      MessageId: receipt,
      ReceiptHandle: receipt,
      MD5OfBody: createHash('md5').update(body).digest('hex'),
      Body: body,
    };
  };
  const sqs = http.createServer(async (request, response) => {
    const chunks = [];
    for await (const chunk of request) chunks.push(chunk);
    const input = JSON.parse(Buffer.concat(chunks).toString());
    const operation = request.headers['x-amz-target']?.split('.').at(-1);
    let output = {};
    if (operation === 'GetQueueAttributes') {
      output = { Attributes: { ApproximateNumberOfMessages: '1', ApproximateNumberOfMessagesNotVisible: '0' } };
    } else if (operation === 'ReceiveMessage') {
      if (!delivered.has(input.QueueUrl)) {
        delivered.add(input.QueueUrl);
        output.Messages = input.QueueUrl.endsWith('/invitations')
          ? [
              sqsMessage(invitation(), 'invitation-ok'),
              sqsMessage(invitation({ InvitationId: 'rejected-invitation', UserEmail: 'reject@example.com' }), 'invitation-failed'),
            ]
          : [sqsMessage(reservation(), 'reservation-ok')];
      } else {
        output.Messages = [];
      }
    } else if (operation === 'DeleteMessage') {
      deleted.add(input.ReceiptHandle);
    } else {
      response.writeHead(400);
      response.end(JSON.stringify({ message: 'Unexpected test operation' }));
      return;
    }
    response.writeHead(200, { 'Content-Type': 'application/x-amz-json-1.0' });
    response.end(JSON.stringify(output));
    maybeComplete();
  });
  context.after(() => { sqs.closeAllConnections(); sqs.close(); });
  const sqsPort = await listen(sqs);
  const reservationSocket = net.createServer();
  const healthPort = await listen(reservationSocket);
  await new Promise((resolve) => reservationSocket.close(resolve));

  const workerEnvironment = testEnvironment({
      EMAIL_DELIVERY_MODE: 'smtp',
      EMAIL_SMTP_HOST: '127.0.0.1',
      EMAIL_SMTP_PORT: String(smtpPort),
      EMAIL_SMTP_USE_SSL: 'false',
      EMAIL_FROM: 'sender@example.com',
      AWS_ENDPOINT_URL: `http://127.0.0.1:${sqsPort}`,
      SQS_QUEUE_URL: `http://127.0.0.1:${sqsPort}/reservations`,
      SQS_INVITATION_QUEUE_URL: `http://127.0.0.1:${sqsPort}/invitations`,
      SQS_WAIT_TIME_SECONDS: '0',
      POLL_INTERVAL_MS: '20',
      HEALTH_PORT: String(healthPort),
    });
  const image = process.env.WORKLY_TEST_IMAGE;
  const containerName = `workly-notification-regression-${process.pid}`;
  const forwardedNames = [
    'EMAIL_DELIVERY_MODE', 'EMAIL_SMTP_HOST', 'EMAIL_SMTP_PORT', 'EMAIL_SMTP_USE_SSL',
    'EMAIL_SMTP_USERNAME', 'EMAIL_SMTP_PASSWORD', 'EMAIL_FROM',
    'AWS_REGION', 'AWS_ACCESS_KEY_ID', 'AWS_SECRET_ACCESS_KEY', 'AWS_SESSION_TOKEN',
    'AWS_EC2_METADATA_DISABLED', 'AWS_ENDPOINT_URL', 'SQS_QUEUE_URL',
    'SQS_INVITATION_QUEUE_URL', 'SQS_WAIT_TIME_SECONDS', 'POLL_INTERVAL_MS',
    'INVITATION_BASE_URL', 'HEALTH_PORT', 'NEW_RELIC_ENABLED', 'NEW_RELIC_LICENSE_KEY',
  ];
  const arguments_ = image
    ? ['run', '--rm', '--network', 'host', '--name', containerName,
        ...forwardedNames.flatMap((name) => ['--env', `${name}=${workerEnvironment[name]}`]), image]
    : ['-r', 'newrelic', 'src/index.js'];
  const worker = spawn(image ? 'podman' : process.execPath, arguments_, {
    cwd: workerRoot,
    env: workerEnvironment,
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  let output = '';
  let errors = '';
  worker.stdout.setEncoding('utf8').on('data', (chunk) => {
    output += chunk;
    if (output.includes('Failed to process message')) failedMessageObserved = true;
    maybeComplete();
  });
  worker.stderr.setEncoding('utf8').on('data', (chunk) => { errors += chunk; });
  context.after(async () => {
    if (worker.exitCode !== null || worker.signalCode !== null) return;
    const exited = once(worker, 'exit');
    worker.kill('SIGTERM');
    const force = setTimeout(() => worker.kill('SIGKILL'), 3000);
    try { await exited; } finally {
      clearTimeout(force);
      if (image) spawnSync('podman', ['rm', '-f', containerName], { stdio: 'ignore' });
    }
  });
  await Promise.race([
    completed,
    once(worker, 'exit').then(([code]) => { throw new Error(`Worker exited ${code}: ${errors}`); }),
    delay(10000, null, { ref: false }).then(() => { throw new Error(`Worker did not process test messages: ${output} ${errors}`); }),
  ]);
  const health = await fetch(`http://127.0.0.1:${healthPort}/health`);
  assert.equal(health.status, 200);
  assert.equal((await health.json()).service, 'notification-worker');
  assert.deepEqual([...deleted].sort(), ['invitation-ok', 'reservation-ok']);
  assert.equal(messages.length, 2);
  const content = messages.join('\n').replace(/=\r\n/g, '').replace(/=3D/gi, '=');
  assert.match(content, /Invitacion a Workly/);
  assert.match(content, /local-only-invitation-token/);
  assert.match(content, /Reservation created/);
  assert.equal(output.includes('local-only-invitation-token'), false);
  assert.doesNotMatch(errors, /license key.*(?:missing|required)/i);
});
