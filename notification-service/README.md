# Notification Service

Asynchronous notification service using **AWS SNS + SQS** and a **dockerized Node.js worker** that sends emails via **Brevo**.

## Architecture

```
Reservation publisher → SNS (notification-topic) → SQS (notification-queue) ─┐
Invitation publisher  → SNS (invitation-topic)  → SQS (invitation-queue)  ─┤→ Worker → Brevo
```

## Requirements

### Recommended: entirely local with Podman

From the **Workly root**, use the central overlay instead of this service's
standalone Compose files:

```bash
python3 scripts/up-local.py --build --messaging
python3 scripts/check-local-invitation.py --reservations
```

This starts account-free LocalStack 4.14.0 (SNS/SQS), Mailpit 1.34.0 (SMTP), and
the worker with dummy AWS credentials. Open **http://localhost:8025** for captured
mail; nothing is delivered to the Internet. No AWS CLI, Brevo key, LocalStack
token, Docker socket or host Node installation is needed. The smoke test creates
a new fictitious user and a new reservation, cancels only that reservation and
keeps the test records. It verifies invitation acceptance/login and both reservation
emails through APIs, not browser automation.

LocalStack resources/events are ephemeral (`PERSISTENCE=0`); Mailpit mail is stored
in a named volume. The overlay also publishes audit events to a FIFO queue. Add
`--audit` to run the optional SQL Server and Audit consumer (more memory).
See [local configuration](../CONFIGURACION_LOCAL.md).

### Standalone / external AWS setup

- Node.js 22.16 or newer when running without containers (New Relic requirement).
- [AWS CLI](https://aws.amazon.com/cli/) configured with valid credentials
- [Docker](https://www.docker.com/) and Docker Compose
- [Brevo](https://www.brevo.com/) API key

## 1. AWS Infrastructure

Paste `scripts/setup-aws.sh` into CloudShell. It creates two SNS → SQS pipelines:

| Resource | Default name |
|----------|--------------|
| Reservation SNS Topic | `notification-topic` |
| Reservation SQS Queue | `notification-queue` |
| Invitation SNS Topic | `invitation-topic` |
| Invitation SQS Queue | `invitation-queue` |

Copy these values into your worker environment:

```
AWS_REGION=us-east-1
SQS_QUEUE_URL=https://sqs.us-east-1.amazonaws.com/...
SQS_INVITATION_QUEUE_URL=https://sqs.us-east-1.amazonaws.com/...
INVITATION_BASE_URL=http://localhost:8080/invitations/accept
```

## 2. Node.js Worker

### Configuration

```bash
cp .env.example .env
```

| Variable | Description |
|----------|-------------|
| `SQS_QUEUE_URL` | Reservation notifications queue |
| `SQS_INVITATION_QUEUE_URL` | Invitation emails queue |
| `INVITATION_BASE_URL` | Backend accept-invitation URL (same as `EMAIL_INVITATION_BASE_URL` in backend) |
| `BREVO_API_KEY` | Brevo API key |
| `BREVO_SENDER_EMAIL` | Verified sender email in Brevo |

On ECS, AWS credentials are provided by **LabRole** (task role). Do not set `AWS_ACCESS_KEY_ID` or `AWS_SECRET_ACCESS_KEY`.

### Run locally

```bash
cd worker
npm ci
npm start
```

### Security and regression checks

The worker uses Brevo SDK 6, Nodemailer 10, the updated SQS SDK and New Relic 14.
Brevo delivery now uses `BrevoClient.transactionalEmails.sendTransacEmail`.
Node 22.16+ and npm 11.6.2 are declared; the container installs the same npm version.

From `worker/`:

```bash
npm ci
npm test
npm audit --omit=dev
```

The dependency review reduced the original 23 npm audit findings to **0 known
findings** in the installed production tree. This is not a guarantee that the
entire application or container is vulnerability-free. Re-run the audit regularly.

The 18 regression tests use the real Brevo SDK with a fake HTTP transport, the
real SQS SDK with a local HTTP simulator, and a local SMTP test server. They check
both queues, invitation and reservation delivery, failed delivery without message
acknowledgement, configuration, and omission of invitation tokens from email logs.
They do not need AWS/Brevo credentials and do not send mail to the Internet.

On Linux, the same worker integration test can run the already-built Podman image
against those simulators instead of starting the worker directly with Node:

```bash
WORKLY_TEST_IMAGE=localhost/workly-notification:local npm test
```

This uses a temporary container with host networking to reach the loopback-only
test servers. Only explicit dummy test settings are forwarded to that container.

`EMAIL_DELIVERY_MODE=smtp` supports SMTP without authentication, such as a local
Mailpit instance. Set `EMAIL_SMTP_HOST`, `EMAIL_SMTP_PORT=1025` and
`EMAIL_SMTP_USE_SSL=false` for a plain local SMTP server. Leave username/password
unset, or provide both. Keep TLS enabled for external SMTP where required.
`EMAIL_DELIVERY_MODE=log` does not connect to a mail provider and does not print
the message body. New Relic is disabled when its license key is absent.

The central Compose keeps its offline default; `--messaging` enables the worker
and the local SNS/SQS/Mailpit integration described above.
See [SECURITY_REVIEW.md](SECURITY_REVIEW.md) for versions, scope and limitations.

### Run with Docker

From the `notification-service` repository root:

```bash
docker compose up --build
```

The compose file starts the Node.js worker using the variables from `.env`.

The worker does not expose a public API, but it starts an internal health server. In the full root compose this health endpoint is configured on port `8080` inside the container.

To stop it:

```bash
docker compose down
```

## Message formats

### Reservation notification

```json
{
  "emails": ["user@example.com", "manager@example.com"],
  "emailSubscribed": true,
  "action": "updated",
  "actionAt": "2026-06-14T10:30:00Z",
  "reservation": { "id": "res-1001", "space": { "name": "Room A" }, "startAt": "...", "endAt": "..." },
  "user": { "id": "user-1", "name": "John", "email": "user@example.com" },
  "changes": [{ "field": "startAt", "previous": "...", "current": "..." }]
}
```

`emails` is required and must contain at least one address. The worker sends one email to each address in the list.

### Invitation email

Published by **workly-api** to `SNS_INVITATION_TOPIC_ARN` when an invitation is created:

```json
{
  "InvitationId": "00000000-0000-0000-0000-000000000001",
  "UserEmail": "user@example.com",
  "CompanyId": "00000000-0000-0000-0000-000000000002",
  "CompanyName": "Acme Corp",
  "AreaName": "Engineering",
  "PlainToken": "token-from-backend"
}
```

Email sent (same as backend `EmailService.SendInvitationEmail`):

- **Subject:** `Invitacion a Workly`
- **Body:** invitation to join `{CompanyName}` in area `{AreaName}` with one-time link `{INVITATION_BASE_URL}?invitationId=...&token=...`

## 3. Cleanup

Edit and paste `scripts/delete.sh` in CloudShell to remove both SNS topics and SQS queues.

## Publish test messages

**Reservation** — edit `scripts/publish-test-message.sh`:

```bash
SNS_TOPIC_ARN="arn:aws:sns:us-east-1:ACCOUNT_ID:notification-topic"
RECIPIENT_EMAILS="your-email@example.com,other@example.com"
ACTION="updated"
```

**Invitation** — edit `scripts/publish-test-invitation.sh`:

```bash
SNS_INVITATION_TOPIC_ARN="arn:aws:sns:us-east-1:ACCOUNT_ID:invitation-topic"
USER_EMAIL="your-email@example.com"
```

## Project structure

```
├── scripts/
│   ├── setup-aws.sh
│   ├── delete.sh
│   ├── publish-test-message.sh
│   └── publish-test-invitation.sh
├── worker/
│   ├── src/
│   │   ├── index.js
│   │   ├── config.js
│   │   ├── processor.js
│   │   ├── templates/
│   │   │   ├── reservationEmail.js
│   │   │   └── invitationEmail.js
│   │   ├── sqs/client.js
│   │   └── services/emailService.js
│   ├── Dockerfile
│   └── package.json
├── docker-compose.yml
└── .env.example
```
