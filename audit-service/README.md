# Audit Service

Independent microservice responsible for administrative and critical-action auditing in Workly.

Business services must not write to the audit database. They publish audit events to AWS SQS and this service consumes the queue, validates each event, and stores an immutable append-only audit record in its own SQL Server database.

## What It Audits

Supported action types are defined in `AuditActionType`:

- `ClientCompanyCreated`
- `UserInvited`
- `UserRegistered`
- `UserAreaAssigned`
- `UserAreaModified`
- `PhysicalSpaceCreated`
- `PhysicalSpaceUpdated`
- `PhysicalSpaceDeleted`
- `ReservationCreated`
- `ReservationUpdated`
- `ReservationCancelled`
- `ApiKeyCreated`
- `ApiKeyRevoked`
- `SystemConfigurationChanged`

## Event Format

Publish this JSON to `SQS_AUDIT_QUEUE_URL`:

```json
{
  "eventId": "11111111-1111-1111-1111-111111111111",
  "occurredAt": "2026-06-14T15:30:00Z",
  "actionType": "ReservationCreated",
  "userId": "22222222-2222-2222-2222-222222222222",
  "userEmail": "admin@workly.test",
  "companyId": "33333333-3333-3333-3333-333333333333",
  "entityType": "Reservation",
  "entityId": "44444444-4444-4444-4444-444444444444",
  "details": { "source": "workly-api" },
  "oldValues": null,
  "newValues": { "status": "Active" },
  "correlationId": "request-correlation-id"
}
```

`details`, `oldValues`, and `newValues` are stored as JSON text fields with SQL Server JSON checks.

## Backend Integration

Company Service publishes audit events through `workly-api/Audit/SqsAuditEventPublisher`. It is best-effort: if SQS is unavailable or not configured, the business request is not rolled back.

## AWS Infrastructure

Paste `scripts/setup-aws.sh` into CloudShell. It creates the FIFO SQS queue consumed by this service:

| Resource | Name |
| --- | --- |
| Audit SQS Queue | `audit-events-queue.fifo` |

At the end, copy the printed values into the audit service and business services environment.

Configure these variables in the private environment for `company-service/backend`:

```powershell
SQS_AUDIT_QUEUE_URL=https://sqs.<region>.amazonaws.com/<account>/<queue-name>
SKIP_AUDIT_QUEUE=false
```

The publisher supports FIFO queues too. If the URL ends with `.fifo`, it sends `MessageGroupId` and `MessageDeduplicationId`.

## Local Configuration

Copy the example environment file and fill real values:

```powershell
Copy-Item .env.example .env
```

Required values:

- `SUPER_SECRET_KEY`: the shared JWT secret used by the business APIs.
- `AWS_REGION`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_SESSION_TOKEN`: AWS credentials. If using AWS Academy or temporary credentials, keep `AWS_SESSION_TOKEN`.
- `SQS_AUDIT_QUEUE_URL`: queue consumed by this service.
- `ConnectionStrings__AuditDatabase`: runtime connection string, preferably using a restricted DB user.
- `ConnectionStrings__AuditMigrationDatabase`: admin connection string used only for schema creation.

## Run With Docker

From the `audit-service` repository root:

```powershell
docker compose up --build
```

The compose file starts SQL Server, runs `dotnet AuditService.Api.dll migrate`, creates the `audit_records` table, and grants a restricted `audit_user` only `SELECT` and `INSERT` over that table while explicitly denying `UPDATE` and `DELETE`.

The API listens on `http://localhost:5151`.

The compose file starts:

- `audit-db`: SQL Server local database.
- `audit-migrate`: schema and permission migration container.
- `audit-api`: audit HTTP API and SQS consumer.

Health check:

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:5151/health
```

To stop the service:

```powershell
docker compose down
```

## Run Locally

Start only SQL Server:

```powershell
docker compose up audit-db
```

Create the schema and restricted database user:

```powershell
dotnet run --project .\src\AuditService.Api\AuditService.Api.csproj -- migrate
```

Run the service:

```powershell
dotnet run --project .\src\AuditService.Api\AuditService.Api.csproj
```

For local API-only testing without SQS, set:

```powershell
$env:SKIP_AUDIT_QUEUE = "true"
```

## Query Audit Records

Only administrators can query logs. Send the same Workly JWT used by the main backend. If the admin token contains `CompanyId`, the API restricts results to that company. A token without company scope can query all companies.

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:5151/api/audit-records?from=2026-06-01T00:00:00Z&to=2026-06-30T23:59:59Z&actionType=ReservationCreated&page=1&pageSize=20" `
  -Headers @{ Authorization = "Bearer $env:ADMIN_JWT" }
```

Supported filters:

- `from`
- `to`
- `userId`
- `actionType`
- `companyId`
- `page`
- `pageSize`

## Immutability Rules

- There are no update or delete endpoints.
- `AuditDbContext` rejects EF update/delete attempts for `AuditRecord`.
- `event_id` is unique, so retried SQS messages are idempotent.
- Runtime database user should have only `SELECT` and `INSERT` on `audit_records`.
- Business services communicate only through SQS and never through the audit database.

## Tests

```powershell
dotnet test .\audit-service.sln
```

## RNF 6 / Integracion continua

El microservicio con CI es `audit-service`.

El RF elegido para RNF 6 es la consulta de registros de auditoria con filtros y scoping por `CompanyId`:

```http
GET /api/audit-records
```

La cobertura focalizada valida el comportamiento unitario de:

- consulta de auditoria;
- filtros;
- scoping por `CompanyId`;
- rechazo cuando un usuario con `CompanyId` intenta consultar otra compania;
- mapping de respuesta.

Las clases objetivo del RF deben mantener 100% de cobertura de lineas y 100% de cobertura de ramas cuando el reporte declare ramas:

- `AuditRecordsController.cs`
- `AuditEventService.cs`
- `AuditRecord.cs`
- `AuditActionTypes.cs`
- `ClaimsPrincipalExtensions.cs`
- `AuditRecordResponse.cs`

El pipeline de GitHub Actions esta definido en `.github/workflows/audit-service-ci.yml`. Corre en `push` y `pull_request` hacia `main`, usa .NET 8 y ejecuta:

```bash
dotnet restore audit-service.sln
dotnet build audit-service.sln --configuration Release --no-restore
dotnet test audit-service.sln --configuration Release --no-build --collect:"XPlat Code Coverage" --results-directory TestResults
python3 scripts/check-rnf6-coverage.py
```

Si fallan el restore, la compilacion, algun test o el coverage gate, el job queda en estado `failed` y no deberia integrarse a la rama principal.

Para ejecutar tests localmente:

```bash
dotnet test audit-service.sln
```

Para generar cobertura localmente:

```bash
dotnet test audit-service.sln --collect:"XPlat Code Coverage" --results-directory TestResults
```

Para ejecutar el coverage gate localmente:

```bash
python3 scripts/check-rnf6-coverage.py
```

El script busca `coverage.cobertura.xml` dentro de `TestResults` o permite recibir la ruta del XML por argumento. Si baja la cobertura de alguna clase objetivo, devuelve error, falla el pipeline y se deben corregir o agregar tests para el RF elegido.

El 100% aplica al RF elegido y a las clases objetivo listadas. No aplica a la cobertura global del microservicio porque hay infraestructura fuera del alcance unitario del RF, como `Program.cs`, `AuditSqsConsumerBackgroundService.cs`, seed, permisos DB, configuracion de autenticacion y codigo generado.

### RNF 6 coverage report

Para generar el reporte HTML/TextSummary de cobertura focalizado en las clases cubiertas por el RF de consulta de auditoria, se uso:

```bash
reportgenerator \
  -reports:"/tmp/audit-service-coverage/*/coverage.cobertura.xml" \
  -targetdir:"/tmp/audit-service-coverage-report-rnf6" \
  -reporttypes:"Html;TextSummary" \
  -filefilters:"+*AuditRecordsController.cs;+*AuditEventService.cs;+*AuditRecord.cs;+*AuditActionTypes.cs;+*ClaimsPrincipalExtensions.cs;+*AuditRecordResponse.cs"
```

El reporte HTML se visualizo abriendo:

```text
/tmp/audit-service-coverage-report-rnf6/index.html
```
