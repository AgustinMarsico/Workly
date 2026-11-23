# Workly: compilación y requisitos de ejecución

## Actualización: LocalStack y Mailpit integrados

`python3 scripts/up-local.py --build --messaging` habilita SNS/SQS, SMTP capturado
y el worker con Podman, sin cuentas externas. Pasó la prueba de invitación,
aceptación y login por APIs, además de creación/cancelación de reservas con sus
correos. Se corrigió la consulta de destinatarios de Reservation hacia Identity.
Se respetan las preferencias de desuscripción existentes.

Las colas de correo quedaron vacías después de entregar; la cola FIFO contiene
eventos de auditoría. Audit se reconstruyó, pero SQL Server/consumidor no se
arrancaron en esta integración. Pasaron 98 tests .NET, 18 del worker (también con
su imagen Podman) y 6 del arranque. `.env` conserva su hash original. Sin commit
ni push. Ver `CONFIGURACION_LOCAL.md` y `VALIDACION_CONTENEDORES.md`.

## Actualización: revisión de seguridad del worker de notificaciones

Se actualizaron sus dependencias y se adaptó el envío a Brevo SDK 6. La auditoría
npm de producción pasó de 23 hallazgos a 0 conocidos, tanto localmente como dentro
de la imagen Podman reconstruida. Pasaron 18 tests, incluida la entrega SMTP y
el consumo de colas con simuladores locales y con el worker ejecutándose en Podman.
Ver `notification-service/SECURITY_REVIEW.md` para versiones, alcance y limitaciones.
Los apartados posteriores conservan el diagnóstico inicial como referencia histórica.
El Compose base mantiene el modo sin notificaciones; el overlay opcional ya
integra LocalStack/Mailpit. No se hicieron commits ni push en esta revisión.

## Actualización: construcción y migraciones verificadas (punto 6)

Las siete imágenes se construyeron con Podman. Las migraciones de las cinco bases
se ejecutaron y repitieron correctamente, verificando seeds y permisos restringidos
en Audit. El frontend y las cuatro APIs de negocio quedaron funcionando, con login
y consultas autenticadas verificadas. Audit/SQL Server se validaron por separado y
se detuvieron para ahorrar memoria; Notification no se arrancó.

Se añadió `scripts/up-local.py` como arranque recomendado por etapas, ya probado,
y dos tests del script. Se ajustaron Dockerfiles, exclusiones de build, imágenes
compartidas y códigos de salida de errores. Más información, comandos y pendientes
en `VALIDACION_CONTENEDORES.md` y `CONFIGURACION_LOCAL.md`.

Además de MySqlConnector, el build del worker detectó 23 vulnerabilidades npm,
incluidas 2 críticas. Se documentaron y no se realizaron actualizaciones mayores
automáticas. El worker sigue desactivado.

## Actualización: punto 3, configuración local centralizada

Se crearon `.env` privado (13 secretos aleatorios, permisos 600), `.env.example`
sin secretos, `.gitignore`, `scripts/init-local-env.py` y `compose.yaml` central.
Los valores no se imprimieron. El generador rechaza sobrescribir un entorno existente.

El Compose entrega a cada servicio sólo las variables necesarias; los usuarios
de las APIs MySQL no son root, y cada base tiene contraseñas independientes.
No se incluyeron credenciales AWS/Brevo/New Relic. Auditoría asíncrona y correos
quedan desactivados; Audit/SQL Server son opcionales mediante el perfil `audit`.
El frontend no recibe secretos. Su Dockerfile ahora acepta también `AUDIT_API_URL`.

Para hacer efectiva la configuración central se preparó el Compose conjunto
(adelanto del punto 5), incluyendo DNS interno, puertos públicos coherentes y
dependencias de migración. Esto incorpora los ajustes de configuración del punto 4,
pero su funcionamiento integrado aún debe probarse. Sólo se validó el modelo con
Podman Compose y se comprobaron secretos compartidos, aislamiento de contraseñas,
flags locales y existencia de contextos de construcción; no se levantó Workly.

Ver `CONFIGURACION_LOCAL.md` para manejo de secretos, comandos y limitaciones.
Las secciones de diagnóstico inicial siguientes describen el estado previo a
estas correcciones; los Compose individuales se conservaron sin modificar.

## Actualización: punto 2 resuelto, lockfile del frontend

Se normalizó `frontend/workly-front/package-lock.json`
con npm 11.6.2, en un directorio temporal sin `node_modules`.
El único cambio es `@emnapi/wasi-threads` de 1.2.2 a 1.2.3, con URL e integridad
obtenidas de npm. No se cambió `package.json` ni se actualizaron otras dependencias.

Verificación en una copia limpia del frontend, con Node 22.23.5 y npm 11.6.2:

- `npm ci`: correcto, 473 paquetes instalados, sin modificar el lockfile.
- `npm run build`: correcto, bundle inicial de producción de 403,48 kB.
- Logs: `/tmp/opencode/workly-investigacion/frontend-lock-fixed-ci.log` y
  `/tmp/opencode/workly-investigacion/frontend-lock-fixed-build.log`.

Los repositorios originales no se modificaron. No se validó aún la construcción
del contenedor; se hará en el punto 6. En el PATH de esta sesión todavía no se
encuentran Node/npm globales, por lo que se usaron las herramientas aisladas.
Siguiente punto: unificar secretos y variables de entorno para desarrollo local.

## Actualización: punto 1 resuelto con Podman

Se verificó Podman 5.8.9 en Fedora 44, rootless, con Netavark/Aardvark DNS y SELinux
habilitado. No había proveedor de Compose; se instaló `podman-compose` 1.6.0
aislado para el usuario mediante `uv tool install podman-compose`.
El ejecutable queda en `~/.local/bin/podman-compose`, ya incluido en PATH.
`podman compose version` ahora funciona y selecciona ese proveedor automáticamente.
No se instaló Docker ni se modificó la configuración global de Podman.

Pruebas reales con un Compose temporal y contenedores BusyBox:

- Descarga de imágenes desde Docker Hub.
- Resolución DNS y HTTP entre servicios por nombre.
- Dependencias `service_healthy` y `service_completed_successfully`.
- Acceso HTTP desde el host a un puerto publicado en 127.0.0.1.
- Acceso HTTP desde un contenedor al host mediante
  `host.docker.internal`, con `extra_hosts: host.docker.internal:host-gateway`.

Los contenedores y la red de prueba se eliminaron. Se conservaron las imágenes
descargadas (Alpine y BusyBox); Workly no fue levantado.
La prueba está en `/tmp/opencode/workly-podman-check/compose.yaml`.

Para el futuro Compose de Workly se usarán nombres completos de imágenes
(`docker.io/library/mysql:8.0`, por ejemplo), comunicación entre servicios por DNS,
puertos locales superiores a 1024 y volúmenes nombrados para las bases. Si se
añaden bind mounts, hay que considerar etiquetas SELinux `:Z`/`:z` según su uso.
La compatibilidad de construcción de los Dockerfiles y de las bases reales sigue
pendiente del punto 6; esta prueba valida la infraestructura básica, no todo Workly.

Comando recomendado: `podman compose ...`, sin `sudo`.
Siguiente punto: corregir el lockfile del frontend.

## Alcance

Se analizaron los servicios dentro de `workly/`, sus README, Dockerfiles,
Compose y documentación técnica. Varios README todavía describían el monolito.
No se modificó código fuente ni configuración de los servicios para corregir problemas.

El equipo no tiene `dotnet`, `node`, `npm` ni `docker` disponibles en su PATH habitual.
Para las pruebas se descargaron herramientas aisladas en `/tmp/opencode/workly-toolchains`:
.NET SDK 8.0.428, Node 22.23.5 y npm 11.6.2 (versión declarada por el frontend).
Las compilaciones generan `bin/`, `obj/` y las instalaciones generan `node_modules/`
dentro de los servicios. Los logs de investigación están en
`/tmp/opencode/workly-investigacion/`.

**Compilar no equivale a levantar el sistema.** No se probaron imágenes Docker,
migraciones contra bases reales ni flujos integrados: falta Docker y la infraestructura.
No se crearon recursos cloud ni se enviaron correos.

## Resultados de compilación

| Proyecto | Resultado |
| --- | --- |
| audit-service | Solución compilada en Release. 13 tests aprobados. |
| company-service | La API y sus dependencias compilan en Release; la solución falla por referencias a tres proyectos de tests ausentes. |
| identity-service | Solución compilada en Release. |
| reservation-service | Solución compilada en Release. |
| rooms-service | API y dependencias compiladas directamente en Release. 85 tests aprobados (32 API, 34 lógica, 19 persistencia). Su solución devuelve éxito sin compilar proyectos. |
| notification-service | `npm ci` correcto y los 8 archivos JS/CJS propios pasan `node --check`; imports de procesamiento/SQS correctos en modo log, sin acceder a AWS. Es JavaScript, no declara script de compilación. Al arrancar sin entorno falla por `SQS_QUEUE_URL` faltante. |
| frontend | Punto 2 resuelto: `npm ci` y build de producción correctos con el lockfile corregido. Bundle inicial: 403,48 kB. |

### Problemas de las soluciones .NET

- `company-service/backend/workly-app.sln` referencia archivos que no existen:
  - `workly-api-test/workly-api-test.csproj`
  - `BusinessLogicTest/BusinessLogicTest.csproj`
  - `DataAccessTest/DataAccessTest.csproj`
  - Alternativa comprobada: compilar `backend/workly-api/workly-api.csproj` directamente,
    como ya hace su Dockerfile. Recuperar los tests o actualizar la solución requiere una decisión posterior.
- `rooms-service/backend/RoomsService.sln` no contiene las
  configuraciones de compilación de los proyectos en su sección `Global`.
  `dotnet build` devuelve 0 y avisa que no encuentra proyectos para restaurar.
  Se verificó la compilación real con `backend/RoomsService.Api/RoomsService.Api.csproj`.

### Frontend: diagnóstico inicial del lockfile (ya corregido en el punto 2)

En la revisión inicial, `npm ci` fallaba tanto con npm 10.9.9 como con npm 11.6.2 (la versión declarada).
Con npm 11 informa que `@emnapi/wasi-threads@1.2.2` del lockfile no satisface
`1.2.3`. Con npm 10 también informa entradas `@emnapi/core` y `@emnapi/runtime`
faltantes. Ese problema bloqueaba el paso `npm ci` del Dockerfile y CI.

La normalización y verificación desde cero ya se completaron con npm 11.6.2;
véase la actualización del punto 2 al inicio de este documento.
Como prueba adicional se intentó instalar con `npm install --package-lock=false`
para poder probar Angular sin persistir una corrección del lockfile.
La instalación y `npm run build` terminaron correctamente con Node 22 y npm 11.6.2.
El resultado está en `frontend/workly-front/dist/workly-front/`.
Esto valida el código Angular con las dependencias resueltas en esta prueba,
pero no sustituye una instalación reproducible con el lockfile corregido.

Comandos de la alternativa usada, desde `workly-front/`:

```bash
npm install --package-lock=false --no-audit --no-fund
npm run build
```

### Advertencias de seguridad

NuGet informa `NU1902` para MySqlConnector 2.4.0 en company, identity y reservation,
y 2.3.7 en rooms: vulnerabilidad moderada
<https://github.com/advisories/GHSA-473q-m89c-ghf8>.
No impide compilar, pero conviene actualizar y volver a probar antes de desplegar.

## Componentes necesarios

Cada servicio ocupa una carpeta con su nombre, sin prefijos personales.

| Componente | Puerto del host según Compose actual | Persistencia / dependencias |
| --- | --- | --- |
| Company API | 5000 | MySQL 8.0, base `worklydb`; Identity por HTTP |
| Identity API | 5100 | MySQL 8.4, base `workly_identity`; Company por HTTP |
| Rooms API | 5300 | MySQL 8.0, base `workly_rooms`; Identity y Reservation por HTTP |
| Reservation API | 8081 | MySQL 8.0, base `workly_reservations`; Company, Identity y Rooms por HTTP |
| Audit API | 5151 | SQL Server 2022, base `workly_audit`; consumidor SQS |
| Notification worker | Sin puerto publicado | Dos colas SQS; Brevo o SMTP; health interno en 8080 |
| Frontend | 8080 con Nginx; 4200 por defecto en desarrollo | Angular 21; acceso del navegador a las cinco APIs |

Puertos de bases publicados: Company `3308`, Reservation `3307`, Rooms `3309`,
Audit `1434`. Identity no publica MySQL al host. Las APIs .NET usan 8080 dentro
de sus contenedores; MySQL usa 3306 y SQL Server 1433.

Para usar los Compose se necesita Docker Engine con el plugin Docker Compose v2,
acceso a los registros de imágenes, NuGet/npm y al repositorio de New Relic durante
la construcción. .NET y Node en el host no son necesarios si todo se ejecuta con Docker.
Para desarrollo sin Docker: .NET SDK 8, Node compatible y servidores de base de datos.
Angular 21 exige Node `^20.19.0 || ^22.12.0 || >=24.0.0`; no alcanza cualquier Node 20.

## Configuración que falta o debe unificarse

1. **JWT compartido:** usar el mismo `SUPER_SECRET_KEY` en las cinco APIs .NET,
   suficientemente largo para HS256 (al menos 32 bytes). Los valores predeterminados
   de los Compose no coinciden entre todos los servicios.
2. **Autenticación interna:** definir el mismo `INTERNAL_SERVICE_KEY` en Company,
   Identity y Reservation, y pasarlo explícitamente a sus contenedores. El código
   utiliza `X-Service-Key`, pero los Compose actuales no pasan esta variable.
   Es importante para llamadas internas que no pueden depender del JWT del usuario.
3. **API keys:** mantener estable `API_KEY_SECRET` en Identity.
4. **Conexiones:** configurar `ConnectionStrings__DefaultConnection` para cada
   servicio MySQL; en Audit separar `ConnectionStrings__AuditMigrationDatabase`
   y `ConnectionStrings__AuditDatabase`. El usuario runtime de Audit debe tener
   SELECT/INSERT, no UPDATE/DELETE; su proceso `migrate` prepara esos permisos.
5. **Reserva → Rooms:** falta pasar `RoomsService__BaseUrl` a Reservation.
   Debe apuntar a `http://host.docker.internal:5300` con los Compose independientes.
   Sin esta variable el código cae a `MainService__BaseUrl` (Company), que ya no es
   dueño de los endpoints de espacios.
6. **Puerto de reservas:** el Compose de Reservation publica `8081`, pero Rooms y
   el generador del frontend asumen `5200`. Mantener 8081 exige configurar
   `RESERVATION_SERVICE_BASE_URL=http://host.docker.internal:8081` en Rooms y
   `RESERVATION_API_URL=http://localhost:8081` en el frontend.
7. **Direcciones restantes entre contenedores:**
   - Company: `IdentityService__BaseUrl=http://host.docker.internal:5100`.
   - Identity: `OrganizationService__BaseUrl=http://host.docker.internal:5000`.
   - Rooms: `IdentityService__BaseUrl=http://host.docker.internal:5100`.
   - Reservation: `MainService__BaseUrl=http://host.docker.internal:5000` e
     `IdentityService__BaseUrl=http://host.docker.internal:5100`.
   - No usar `localhost` para alcanzar otro contenedor. Los Compose independientes
     ya añaden `host.docker.internal:host-gateway` donde corresponde.
8. **Frontend Docker:** el Compose sólo pasa las URLs de Company y Reservation.
   El Dockerfile asigna Identity y Rooms a la URL de Company por defecto.
   Hay que pasar `IDENTITY_API_URL=http://localhost:5100` y
   `ROOMS_API_URL=http://localhost:5300` como argumentos de build. Para Audit, el
   generador permite `AUDIT_API_URL`, pero el Dockerfile no declara ese argumento;
   actualmente depende del valor por defecto localhost:5151.
   Estas URLs son públicas, usadas por el navegador, no nombres DNS de contenedores.
9. **Orden de migraciones:** Company tiene contenedores `migrate` y `api`, pero
   la API sólo depende de la salud de MySQL, no de que termine `migrate`.
   Conviene ejecutar la migración primero o añadir esa dependencia. El README indica
   ejecutar `dotnet run -- migrate` desde DataAccess, pero el comando está implementado
   en la API. Identity, Rooms y Audit tienen tareas de migración separadas;
   Reservation migra automáticamente al iniciar.
10. **Archivos `.env`:** no hay archivos locales reales, sólo ejemplos en algunos
    repositorios. Un `.env` usado para interpolar Compose no se transmite completo
    al contenedor: hay que declararlo en `environment` o `env_file`.
    En particular, Reservation no pasa `SKIP_AUDIT_QUEUE`, la URL de auditoría ni
    las credenciales AWS; Identity fija las notificaciones y auditoría desactivadas;
    Rooms no pasa la configuración AWS para habilitar auditoría.

## Dos niveles de ejecución

### A. Entorno mínimo sin servicios cloud

Requiere las cuatro bases MySQL, las cuatro APIs de negocio y el frontend.
Audit es opcional para los flujos principales; si se quiere consultar su API,
también se necesita SQL Server y su migración.

- Configurar URLs y secretos anteriores.
- Pasar `SKIP_AUDIT_QUEUE=true` en Company, Identity, Rooms y Reservation.
- Pasar `SKIP_INVITATION_NOTIFICATION=true` en Identity.
- Pasar `SKIP_RESERVATION_NOTIFICATION=true` en Reservation.
- Si se levanta Audit sin SQS, pasarle `SKIP_AUDIT_QUEUE=true`.
- No levantar Notification si no hay colas y servicio de correo configurados.

Esto permite probar el negocio sin AWS, pero **no envía invitaciones por correo ni
genera auditoría asíncrona**. No representa el sistema completo.

### B. Sistema completo con mensajería y correos

Además del entorno mínimo, se requieren:

- Un topic SNS de reservas y otro de invitaciones.
- Dos colas SQS suscritas a esos topics, con sus políticas de acceso.
- Una cola SQS de auditoría, soportada como FIFO (`audit-events-queue.fifo`).
- Credenciales AWS con permisos para publicar/consumir según cada servicio
  (o task roles en ECS); región y token de sesión si las credenciales son temporales.
- `SNS_RESERVATION_TOPIC_ARN` en Reservation y `SNS_INVITATION_TOPIC_ARN` en Identity.
- `SQS_AUDIT_QUEUE_URL` en los publicadores y en Audit, sin los flags de omisión activos.
- Notification: `SQS_QUEUE_URL`, `SQS_INVITATION_QUEUE_URL`, `INVITATION_BASE_URL`,
  `BREVO_API_KEY`, `BREVO_SENDER_EMAIL` y región/credenciales AWS.
  Alternativamente, `EMAIL_DELIVERY_MODE=smtp` y las variables SMTP exigidas por
  `worker/src/config.js`. El código también admite `AWS_ENDPOINT_URL` para el
  cliente SQS del worker, pero eso por sí solo no configura un entorno AWS local
  completo para los publicadores .NET.
- Para probar el worker sin entregar correos, el código admite
  `EMAIL_DELIVERY_MODE=log`; sigue necesitando las dos colas SQS.
- `INVITATION_BASE_URL=http://localhost:8080/invitations/accept` si se usa el frontend Docker.
- New Relic y su licencia son necesarios si se quiere observabilidad externa;
  no son necesarios para compilar ni para probar el negocio local.

Los scripts `scripts/setup-aws.sh` de Audit y Notification describen la creación
de la infraestructura. No fueron ejecutados: crean recursos en una cuenta AWS.

## Secuencia de arranque propuesta (pendiente de validar)

Después de corregir la configuración anterior:

1. Levantar las bases y esperar a que estén listas.
2. Ejecutar migraciones y seeds de Company, Identity, Rooms y, si aplica, Audit.
3. Levantar Company e Identity; luego Rooms y Reservation con sus URLs correctas.
4. Si se requiere el sistema completo, habilitar publicadores y levantar Audit y Notification.
5. Compilar/levantar el frontend con las cinco URLs públicas.
6. Verificar `/health` en Company, Rooms, Reservation y Audit, y `/api/health` en Identity.
7. Probar login, listado de empresas/espacios, reserva y cancelación; después
   invitaciones y auditoría si están habilitadas. Un health correcto no valida todo el flujo.

No existe un Compose global en `workly/`, aunque algunos README lo mencionan.
Los Compose existentes se ejecutan por separado desde cada repositorio; Company
lo tiene en `backend/`. No conviene ejecutarlos todos sin los ajustes indicados.
La documentación anterior mencionaba `/api/audit-logs`, pero el endpoint real
del código y del cliente frontend es `/api/audit-records`.

Los seeds de Identity incluyen usuarios locales `admin@saas.com`,
`usuario@saas.com` y `operaciones@saas.com`, con contraseña de prueba `Password1`.
Company crea una empresa de demostración y sus áreas con IDs compatibles. Usar estas credenciales sólo
para desarrollo y cambiarlas/eliminarlas antes de un despliegue público.

## Comandos de compilación reproducibles

Ejecutar desde `workly/` con .NET y Node/npm en PATH:

```bash
dotnet build audit-service/audit-service.sln -c Release
dotnet build company-service/backend/workly-api/workly-api.csproj -c Release
dotnet build identity-service/backend/IdentityService.sln -c Release
dotnet build reservation-service/WorklyReservationService.sln -c Release
dotnet build rooms-service/backend/RoomsService.Api/RoomsService.Api.csproj -c Release
```

Para reutilizar las herramientas temporales de esta sesión:

```bash
export DOTNET_ROOT=/tmp/opencode/workly-toolchains/dotnet
export DOTNET_CLI_HOME=/tmp/opencode/workly-toolchains/dotnet-home
export NUGET_PACKAGES=/tmp/opencode/workly-toolchains/nuget
export PATH="$DOTNET_ROOT:/tmp/opencode/workly-toolchains/node/node-v22.23.5-linux-x64/bin:$PATH"
# npm 11.6.2 aislado:
node /tmp/opencode/workly-toolchains/npm11/node_modules/npm/bin/npm-cli.js --version
```

Las herramientas en `/tmp` son temporales; no se configuraron globalmente.
