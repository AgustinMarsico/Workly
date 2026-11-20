# Notification Service

Asynchronous notification service using **AWS SNS + SQS** and a **dockerized Node.js worker** that sends emails via **Brevo**.

## Architecture

```
Reservation publisher → SNS (notification-topic) → SQS (notification-queue) ─┐
Invitation publisher  → SNS (invitation-topic)  → SQS (invitation-queue)  ─┤→ Worker → Brevo
```

## Requirements

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
npm install
npm start
```

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
