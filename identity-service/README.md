# Workly Identity Service

Independent service for Workly identity and access lifecycle.

## Owns

- Users and password hashes.
- Roles.
- Invitations and one-time invitation tokens.
- API keys and API key validation.
- Login and JWT generation.
- User associations to `companyId` and `areaId`.

## Does not own

- Company data.
- Area data.
- Spaces.
- Reservations.
- Dashboards or reporting.

Companies and areas belong to the Organization capability. This service stores only `companyId` and `areaId` references. Invitation creation validates those references through the Organization API, but invitation state remains here because accepting an invitation creates credentials, assigns the role, and associates the future user with a company and area.

## Endpoints

- `POST /api/auth/login`
- `GET /api/users`
- `GET /api/users/company`
- `GET /api/users/{id}`
- `GET /api/users?areaId={areaId}` — users in an area with `emailSubscribed` from notification preferences (Member). Without `areaId`, lists all users (Admin only).
- `POST /api/users`
- `POST /api/invitations`
- `GET /api/invitations/{invitationId}`
- `POST /api/invitations/accept`
- `GET /api/api-keys/validate`
- `GET /api/api-keys`
- `POST /api/api-keys`
- `DELETE /api/api-keys/{id}`
- `GET /api/health`

## Local run

From the repository root:

```powershell
docker compose up --build identity-db identity-migrate identity-api
```

The API listens on `http://localhost:5100` in the root compose file.

Required configuration:

- `ConnectionStrings__DefaultConnection`
- `SUPER_SECRET_KEY`
- `API_KEY_SECRET`
- `OrganizationService__BaseUrl`

## Run with Docker Compose

This repository includes its own `docker-compose.yml`, so the identity service can be started independently from the service repository root:

```powershell
docker compose up --build
```

The compose file starts:

- `identity-db`: MySQL database.
- `identity-migrate`: schema migration container.
- `identity-api`: HTTP API.

The API is exposed at:

```text
http://localhost:5100
```

Health check:

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:5100/api/health
```

To stop and remove containers:

```powershell
docker compose down
```
