import assert from 'node:assert/strict';
import { test } from 'node:test';
import { invitation, reservation, testEnvironment } from '../support/fixtures.js';
import { buildInvitationHtml, buildInvitationLink } from '../src/templates/invitationEmail.js';

Object.assign(process.env, testEnvironment());
const { parseSnsMessage, processMessage } = await import('../src/processor.js');

test('SNS envelopes and raw SQS JSON payloads are both accepted', () => {
  const payload = invitation();
  assert.deepEqual(parseSnsMessage(JSON.stringify(payload)), payload);
  assert.deepEqual(parseSnsMessage(JSON.stringify({ Type: 'Notification', Message: JSON.stringify(payload) })), payload);
});

test('invalid queue JSON fails instead of being treated as delivered', () => {
  assert.throws(() => parseSnsMessage('{invalid-json'), SyntaxError);
});

test('invitations keep their metadata and do not log their one-time token', async (context) => {
  const logs = [];
  context.mock.method(console, 'log', (...args) => logs.push(args));
  const payload = invitation();
  const result = await processMessage(payload);
  assert.equal(result.type, 'invitation');
  assert.equal(result.invitationId, payload.InvitationId);
  assert.equal(result.email, payload.UserEmail);
  assert.equal(JSON.stringify(logs).includes(payload.PlainToken), false);
});

test('invitation URLs encode parameters and HTML escapes external text', () => {
  const link = buildInvitationLink('http://localhost/invitations?source=test', 'id with spaces', 'token&value');
  assert.equal(new URL(link).searchParams.get('invitationId'), 'id with spaces');
  assert.equal(new URL(link).searchParams.get('token'), 'token&value');
  const html = buildInvitationHtml({ companyName: '<script>demo</script>', areaName: 'A&B', invitationLink: link });
  assert.equal(html.includes('<script>'), false);
  assert.match(html, /A&amp;B/);
});

for (const action of ['created', 'updated', 'cancelled']) {
  test(`reservation ${action} remains processable after dependency updates`, async (context) => {
    context.mock.method(console, 'log', () => {});
    const result = await processMessage(reservation({
      action,
      ...(action === 'updated' ? { changes: [{ field: 'startAt', previous: 'before', current: 'after' }] } : {}),
    }));
    assert.equal(result.type, 'reservation');
    assert.equal(result.action, action);
  });
}

test('unsubscribed recipients are skipped without sending mail', async (context) => {
  const logger = context.mock.method(console, 'log', () => {});
  const result = await processMessage(reservation({ emailSubscribed: false }));
  assert.equal(result.status, 'skipped');
  assert.equal(logger.mock.callCount(), 0);
});

test('invalid messages fail before they can be acknowledged', async () => {
  await assert.rejects(processMessage(invitation({ AreaName: '' })), /AreaName is required/);
  await assert.rejects(processMessage(reservation({ emails: [] })), /recipient email/);
});
