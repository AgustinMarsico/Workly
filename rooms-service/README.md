# Workly Rooms Service

## Levantar con Docker Compose

El servicio de espacios se levanta con Docker Compose desde la raiz de este repositorio (`rooms-service`), usando el `docker-compose.yml` incluido.

Desde la raiz de `rooms-service`:

```powershell
docker compose up --build
```

El compose levanta:

- `rooms-db`: base MySQL local.
- `rooms-migrate`: contenedor que ejecuta migraciones.
- `rooms-service`: API de espacios.

La API queda publicada en:

```text
http://localhost:5300
```

El contenedor usa internamente el puerto `8080`. Por defecto, para integrarse con otros servicios locales fuera de este compose, utiliza:

- `IDENTITY_SERVICE_BASE_URL=http://host.docker.internal:5100`
- `RESERVATION_SERVICE_BASE_URL=http://host.docker.internal:5200`

Si se quiere cambiar algun valor, crear o editar el archivo `.env` del repositorio antes de levantar el compose.

Tambien puede levantarse desde el compose raiz del workspace junto con el resto de los servicios:

```powershell
docker compose up --build rooms-db rooms-migrate rooms-service
```

Health check:

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:5300/health
```

Para detener los contenedores:

```powershell
docker compose down
```

## Unit tests

The unit tests for this service run locally with MSTest and do not require Docker, a real MySQL database, SQS/AWS, or external HTTP services.

Testing strategy:

- Controller and business logic tests use Moq with strict mocks.
- DataAccess tests do not mock repositories.
- DataAccess tests use SQLite in-memory, a real `WorklyDbContext`, and a real `RoomRepository`.

Run the test projects manually from the repository root:

```bash
dotnet test "RoomsService.ApiTest/RoomsService.ApiTest.csproj"
dotnet test "BusinessLogicTest/BusinessLogicTest.csproj"
dotnet test "DataAccessTest/DataAccessTest.csproj"
```

Expected test count:

- `RoomsService.ApiTest`: 32 tests
- `BusinessLogicTest`: 34 tests
- `DataAccessTest`: 19 tests
- Total: 85 tests

## Coverage report

Coverage is collected with the `XPlat Code Coverage` collector and the visual RNF 6 report is generated with ReportGenerator. The report output is written outside the repository under `/tmp/opencode`.

Manual coverage collection:

```bash
rm -rf /tmp/opencode/rooms-coverage /tmp/opencode/rooms-coverage-report-rnf6
mkdir -p /tmp/opencode/rooms-coverage

dotnet test "RoomsService.ApiTest/RoomsService.ApiTest.csproj" \
  --collect:"XPlat Code Coverage" \
  --results-directory "/tmp/opencode/rooms-coverage/api"

dotnet test "BusinessLogicTest/BusinessLogicTest.csproj" \
  --collect:"XPlat Code Coverage" \
  --results-directory "/tmp/opencode/rooms-coverage/business"

dotnet test "DataAccessTest/DataAccessTest.csproj" \
  --collect:"XPlat Code Coverage" \
  --results-directory "/tmp/opencode/rooms-coverage/data"
```

Install ReportGenerator if needed:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Or update it if it is already installed:

```bash
dotnet tool update -g dotnet-reportgenerator-globaltool
```

Make sure the .NET global tools directory is available in `PATH`:

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```

Generate the filtered visual RNF 6 report:

```bash
reportgenerator \
  -reports:"/tmp/opencode/rooms-coverage/**/coverage.cobertura.xml" \
  -targetdir:"/tmp/opencode/rooms-coverage-report-rnf6" \
  -reporttypes:"Html;TextSummary" \
  -filefilters:"+*RoomsController.cs;+*RoomService.cs;+*RoomRepository.cs;+*ReservationServiceAuthContext.cs;+*RoomTypeFactory.cs;+*RoomType.cs;+*ApiErrors.cs;+*CustomException.cs"
```

Open the HTML report:

```bash
xdg-open /tmp/opencode/rooms-coverage-report-rnf6/index.html
```

Read the text summary:

```bash
cat /tmp/opencode/rooms-coverage-report-rnf6/Summary.txt
```

### Coverage script

The same filtered report can be generated with:

```bash
./scripts/generate-coverage-report.sh
```

To generate and open the HTML report automatically:

```bash
./scripts/generate-coverage-report.sh --open
```

## Continuous integration

This microservice has a GitHub Actions workflow for unit tests and the RNF 6 coverage gate:

```text
.github/workflows/rooms-service-ci.yml
```

The workflow runs on:

- push to `main`
- push to `develop`
- pull request targeting `main`
- pull request targeting `develop`

The CI job runs on Ubuntu with .NET 8 and executes:

- restore for `RoomsService.ApiTest`, `BusinessLogicTest`, and `DataAccessTest`
- build for the three test projects in `Release`
- tests with `XPlat Code Coverage` for the three test projects
- the RNF 6 coverage gate with `python3 scripts/check-rnf6-coverage.py`

The coverage gate is intentionally filtered to the RNF 6 target files:

- `RoomsController.cs`
- `RoomService.cs`
- `RoomRepository.cs`
- `ReservationServiceAuthContext.cs`
- `RoomTypeFactory.cs`
- `RoomType.cs`
- `ApiErrors.cs`
- `CustomException.cs`

It requires 100% line coverage for those files and 100% branch coverage when branches are reported in the Cobertura XML. It does not enforce global service coverage and does not target infrastructure such as `Program.cs`, DI setup, Swagger, Serilog, migrations, Docker, MySQL, real HTTP clients, SQS/AWS, or authentication middleware.

If restore, build, tests, or the RNF 6 coverage gate fail, the CI job fails and the change should not be integrated until fixed.

The CI tests do not require Docker, a real MySQL database, SQS/AWS, or external HTTP services.
