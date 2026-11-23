# Revisión de dependencias del worker

Fecha: 2026-11-20.

## Resultado

La auditoría npm de las dependencias de producción pasó de **23 hallazgos**
(2 críticos, 5 altos, 14 moderados y 2 bajos) a **0 hallazgos conocidos**.
Se verificó tanto con una instalación limpia `npm ci` como dentro de la imagen
`localhost/workly-notification:local` reconstruida con Podman.

Esto describe los avisos disponibles al momento de la revisión, no una garantía de
seguridad de todo el sistema. No es un análisis de vulnerabilidades del sistema
operativo de la imagen ni de las dependencias de las APIs .NET.

## Dependencias resueltas en el lockfile

| Paquete | Versión comprobada |
| --- | --- |
| `@aws-sdk/client-sqs` | 3.1173.0 |
| `@getbrevo/brevo` | 6.2.0 |
| `dotenv` | 17.4.2 (sin cambio de versión mayor) |
| `newrelic` | 14.13.0 |
| `nodemailer` | 10.6.1 |
| `@grpc/grpc-js` (transitiva) | 1.14.7 |
| `protobufjs` (transitiva) | 7.6.6 |

El SDK antiguo de Brevo incorporaba `request`, `form-data` y otras dependencias
obsoletas. Se migró explícitamente a `BrevoClient.transactionalEmails.sendTransacEmail`.
Las actualizaciones transitivas finales respetan los rangos semánticos de sus
dependencias. No se utilizó `--force` ni se añadieron `overrides`.

El árbol instalado se redujo de 320 a 172 paquetes. Se declara Node **22.16+** por
el requisito de New Relic y npm **11.6.2** para reproducir la instalación, también
en el Dockerfile. No se amplió la actualización a otros microservicios.

## Cambios de compatibilidad y protección

- El cliente Brevo se crea sólo en modo `brevo`; SMTP y log no dependen de claves
  de ese proveedor.
- SMTP admite servidores locales sin autenticación. Cuando se usa autenticación,
  se exige configurar tanto usuario como contraseña.
- El modo SMTP mantiene TLS requerido por defecto; para un servidor local plano
  se debe seleccionar explícitamente `EMAIL_SMTP_USE_SSL=false`.
- Los modos desconocidos se rechazan al cargar la configuración.
- El modo log no imprime el cuerpo del correo, que puede contener tokens de invitación.
- El adaptador Brevo no vuelca objetos HTTP completos ni headers al registrar errores.
- New Relic no habilita el agente sin licencia; se conserva la precarga para
  instrumentación cuando sí se configura.

## Pruebas

Se añadieron y aprobaron **18 tests** con el runner nativo de Node, sin nuevas
dependencias de testing:

- Contrato HTTP del SDK real de Brevo 6 con `fetch` simulado, sin contactar al proveedor.
- Configuración y errores de SMTP, con/sin autenticación y TLS.
- Modo log sin clientes externos ni tokens de invitación en la salida.
- Lectura de sobres SNS y mensajes SQS directos.
- Invitaciones y reservas creadas, modificadas y canceladas.
- Destinatarios desuscritos y mensajes inválidos.
- Configuraciones incompletas o inválidas.
- Worker real consumiendo las dos colas mediante el SDK real de SQS frente a un
  simulador HTTP local y entregando correos a un servidor SMTP local.
- Entregas fallidas que **no** generan `DeleteMessage`.
- Endpoint `/health` del worker.

La suite también pasó usando la imagen Podman como proceso worker en la prueba
de integración. No se usaron credenciales reales ni se enviaron correos a Internet.

Desde `notification-service/worker/`:

```bash
npm ci
npm test
npm audit --omit=dev

# Linux: usar la imagen previamente construida en el test de integración.
WORKLY_TEST_IMAGE=localhost/workly-notification:local npm test
```

Informes de esta ejecución: `/tmp/opencode/workly-notification-security/`, fuera
del repositorio. No se prepararon archivos con `git add`, ni se crearon commits o push.

## Integración local posterior

El overlay `compose.messaging.yaml`, activado con `--messaging`, integra LocalStack,
Mailpit y el worker sin secretos externos. Pasó la prueba completa mediante APIs:
invitación → SNS/SQS → correo en Mailpit → aceptación → login; también se rechazaron
un token inválido y la reutilización del enlace. Con un usuario nuevo suscrito,
pasaron la creación/cancelación de una reserva y sus dos correos. Se corrigió el
cliente de destinatarios de Reservation para consultar Identity, no Company.

Ambas colas de correo se drenaron; se comprobaron 16 eventos en la FIFO de
auditoría. El consumidor Audit y SQL Server no se arrancaron en esta integración.
No se automatizó el navegador. La regresión volvió a pasar (18 tests del worker,
también con imagen Podman; 6 del arranque y 98 .NET); la imagen sigue con 0
hallazgos npm conocidos. Gitleaks no encontró secretos en los archivos publicables.

## Pendiente

El envío real mediante Brevo y la telemetría de New Relic requieren validación
separada con credenciales autorizadas; no fueron activados en esta revisión.
