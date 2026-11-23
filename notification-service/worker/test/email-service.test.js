import assert from 'node:assert/strict';
import { test } from 'node:test';
import { BrevoClient } from '@getbrevo/brevo';
import { capturingLogger, emailSettings, testEnvironment } from '../support/fixtures.js';

Object.assign(process.env, testEnvironment());
const { createEmailService } = await import('../src/services/emailService.js');
const message = {
  to: 'recipient@example.com',
  subject: 'Test notification',
  htmlContent: '<p>Hello Workly</p>',
};

test('Brevo 6 sends the expected request using its actual SDK and a fake fetch', async () => {
  const requests = [];
  const settings = emailSettings('brevo');
  const send = createEmailService(settings, {
    logger: capturingLogger(),
    createBrevoClient: (options) => new BrevoClient({
      ...options,
      maxRetries: 0,
      fetch: async (url, options) => {
        requests.push({ url, options });
        return new Response(JSON.stringify({ messageId: 'brevo-test-id' }), {
          status: 201,
          headers: { 'Content-Type': 'application/json' },
        });
      },
    }),
  });
  assert.equal((await send(message)).messageId, 'brevo-test-id');
  assert.equal(requests.length, 1);
  assert.match(String(requests[0].url), /\/v3\/smtp\/email$/);
  assert.equal(new Headers(requests[0].options.headers).get('api-key'), settings.brevo.apiKey);
  assert.deepEqual(JSON.parse(requests[0].options.body), {
    sender: { email: 'sender@example.com', name: 'Workly' },
    to: [{ email: 'recipient@example.com' }],
    subject: message.subject,
    htmlContent: message.htmlContent,
    textContent: 'Hello Workly',
  });
});

test('Brevo errors propagate without logging HTTP headers or response bodies', async () => {
  const logger = capturingLogger();
  const failure = Object.assign(new Error('Delivery rejected'), {
    statusCode: 401,
    response: { headers: { 'api-key': 'private-header-for-test' } },
    body: { token: 'private-response-for-test' },
  });
  const send = createEmailService(emailSettings('brevo'), {
    logger,
    createBrevoClient: () => ({ transactionalEmails: { sendTransacEmail: async () => { throw failure; } } }),
  });
  await assert.rejects(send(message), (error) => error === failure);
  assert.deepEqual(logger.entries, [['Brevo delivery failed', { statusCode: 401 }]]);
});

test('SMTP supports a local server without authentication or TLS', async () => {
  let options;
  let submitted;
  const send = createEmailService(emailSettings('smtp'), {
    logger: capturingLogger(),
    createBrevoClient: () => assert.fail('SMTP must not create a Brevo client'),
    createTransport: (value) => {
      options = value;
      return { sendMail: async (value) => { submitted = value; return { messageId: 'smtp-test' }; } };
    },
  });
  assert.equal((await send(message)).messageId, 'smtp-test');
  assert.equal(options.secure, false);
  assert.equal(options.requireTLS, false);
  assert.equal(Object.hasOwn(options, 'auth'), false);
  assert.equal(submitted.text, 'Hello Workly');
  assert.deepEqual(submitted.from, { address: 'sender@example.com', name: 'Workly' });
});

test('SMTP preserves authenticated TLS configuration and explicit plain text', async () => {
  const settings = emailSettings('smtp');
  Object.assign(settings.smtp, { port: 465, useSsl: true, username: 'test-user', password: 'test-password' });
  let options;
  let submitted;
  const send = createEmailService(settings, {
    logger: capturingLogger(),
    createTransport: (value) => {
      options = value;
      return { sendMail: async (value) => { submitted = value; return {}; } };
    },
  });
  await send({ ...message, textContent: 'Explicit text' });
  assert.equal(options.secure, true);
  assert.equal(options.requireTLS, true);
  assert.deepEqual(options.auth, { user: 'test-user', pass: 'test-password' });
  assert.equal(submitted.text, 'Explicit text');
});

test('SMTP failures propagate so that queue messages can be retried', async () => {
  const failure = new Error('SMTP unavailable');
  const send = createEmailService(emailSettings('smtp'), {
    logger: capturingLogger(),
    createTransport: () => ({ sendMail: async () => { throw failure; } }),
  });
  await assert.rejects(send(message), (error) => error === failure);
});

test('log mode does not create clients or print invitation tokens', async () => {
  const logger = capturingLogger();
  const send = createEmailService(emailSettings('log'), {
    logger,
    createTransport: () => assert.fail('Log mode must not connect to SMTP'),
    createBrevoClient: () => assert.fail('Log mode must not connect to Brevo'),
  });
  assert.deepEqual(await send({ ...message, textContent: 'local-only-invitation-token' }), {
    skipped: true,
    mode: 'log',
  });
  assert.equal(JSON.stringify(logger.entries).includes('local-only-invitation-token'), false);
});

test('unsupported modes are rejected rather than falling back to a provider', () => {
  assert.throws(() => createEmailService(emailSettings('unknown')), /Unsupported email delivery mode/);
});
