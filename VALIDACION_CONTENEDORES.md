# Validación de imágenes y migraciones con Podman

## Resultado

Se construyeron correctamente las **siete imágenes**:

- `localhost/workly-company:local`
- `localhost/workly-identity:local`
- `localhost/workly-rooms:local`
- `localhost/workly-reservation:local`
- `localhost/workly-audit:local`
- `localhost/workly-frontend:local`
- `localhost/workly-notification:local`

El frontend y las cuatro APIs de negocio quedaron **en ejecución**, junto con sus
cuatro bases MySQL. Audit y SQL Server se validaron por separado y se detuvieron
para ahorrar memoria. Notification sólo se construyó; no se arrancó ni se enviaron
correos. No se crearon recursos AWS.

Los logs de construcción y comprobaciones están en `/tmp/opencode/workly-containers/`.
Los logs de comandos Compose de ejecución se guardaron con secretos redactados.
No se modificaron los repositorios originales.

## Correcciones realizadas

- Node 22 para frontend y worker; frontend usa npm 11.6.2, como declara su proyecto.
- Imágenes Node/Nginx con nombres de registro explícitos para Podman.
- New Relic opcional mediante `INSTALL_NEW_RELIC`; el Compose local usa `false`
  y desactiva profiling. La rama de instalación opcional usa keyring firmado, no
  `apt-key`. La configuración final validada fue **sin New Relic**.
- `.dockerignore` en los contextos para excluir archivos privados y artefactos.
- Misma imagen para la API y la migración de cada servicio.
- Company, Identity, Rooms y Audit devuelven código **1** si falla el arranque o
  migración; antes capturaban la excepción sin marcar fallo del proceso.
- Tiempo de parada de las bases ampliado a 60 segundos. SQL Server no alcanzó a
  detenerse dentro del límite previo de 10 segundos.
- Script `scripts/up-local.py` para arranque por etapas y comprobación explícita
  del resultado de migraciones. Se observó un timeout al ejecutar el arranque
  conjunto directo con podman-compose 1.6.0; el script evita depender de ese orden
  implícito. Fue probado de extremo a extremo con el modo local sin Audit.

## Migraciones y seeds

| Base | Validación |
| --- | --- |
| Company / MySQL 8.0 | 15 migraciones registradas; 1 empresa y 3 áreas |
| Identity / MySQL 8.4 | 2 migraciones; 3 usuarios y 3 preferencias |
| Rooms / MySQL 8.0 | 1 migración; 3 espacios |
| Reservation / MySQL 8.0 | 2 migraciones; 5 reservas y tabla de caché de destinatarios |
| Audit / SQL Server 2022 | Esquema creado con `EnsureCreated`; 3 eventos seed |

Las cinco tareas de migración finalizaron con código 0. Se repitieron y no
duplicaron datos. Las cantidades e historiales MySQL se comprobaron nuevamente
después de reiniciar el entorno. Las aplicaciones MySQL utilizan usuarios propios
de sus bases, no root.

En Audit se comprobó con el usuario restringido que:

- `SELECT`: permitido.
- `INSERT`: permitido.
- `UPDATE`: denegado.
- `DELETE`: denegado.

También se verificó la consulta HTTP de Audit: health 200, consulta con JWT local
de prueba 200 y 3 resultados, consulta sin token 401. No se probó consumo de SQS.

Como prueba negativa, las imágenes Company, Identity, Rooms y Audit se ejecutaron
sin red y con configuración inválida: las cuatro devolvieron código 1.

## Comprobaciones HTTP del entorno de negocio

- Company `/health`: 200.
- Identity `/api/health`: 200.
- Rooms `/health`: 200.
- Reservation `/health`: 200 (después de su migración automática).
- Frontend `/`: 200.
- Login real en Identity con administrador y miembro seed: 200.
- Consulta autenticada de empresas: 200, 1 empresa.
- Consulta autenticada de usuarios: 200, 3 usuarios.
- Consulta autenticada de espacios: 200, 3 espacios.
- Consulta autenticada de reservas del miembro: 200, 3 reservas.
- Las cinco URLs del `env.js` servido coinciden con el `.env` central.
- Nginx entrega Angular también al entrar directamente a `/admin`,
  `/app/reservations` e `/invitations/accept`.

Son pruebas de arranque y consultas, no una validación exhaustiva de negocio ni
de la interfaz en un navegador. Falta probar altas/modificaciones/cancelaciones,
API keys, invitaciones completas, aislamiento entre empresas y mensajería.

## Cómo usarlo

Desde `workly/`:

```bash
# Arranque comprobado (imágenes ya construidas):
python3 scripts/up-local.py

# Reconstruir y arrancar:
python3 scripts/up-local.py --build

# Incluir la API de auditoría y SQL Server, si hay memoria suficiente:
python3 scripts/up-local.py --audit

# Ver estado:
podman compose --env-file .env -f compose.yaml ps

# Detener todo, incluido el perfil opcional, conservando datos:
podman compose --env-file .env -f compose.yaml --profile audit down

# Tests del script (no arrancan contenedores):
python3 scripts/test_up_local.py
```

Las etapas comprueban bases saludables, códigos de salida de migraciones y
respuestas HTTP antes de anunciar que el sistema está listo. Los tests del
script verifican orden, interrupción por fallos y redacción de secretos.
No se elimina ningún volumen ante fallos.

Acceso: **http://localhost:8080**. Credenciales **sólo de desarrollo**:

- Administrador: `admin@saas.com` / `Password1`.
- Miembro: `usuario@saas.com` / `Password1`.

No usar estos usuarios en un entorno público. Bases y APIs están publicadas según
`CONFIGURACION_LOCAL.md`; las bases no exponen puertos al host.

## Validación posterior: mensajería local (2026-11-20)

- Arranque con `--build --messaging`: LocalStack 4.14.0, Mailpit 1.34.0 y worker
  saludables; frontend y cuatro APIs de negocio con HTTP 200. Sin socket Docker,
  token de LocalStack ni credenciales externas. Imagen de Audit reconstruida.
- Dos topics, tres colas y dos suscripciones; inicialización repetida sin duplicados.
- `python3 scripts/check-local-invitation.py --reservations`: invitación, entrega
  en Mailpit, enlace local, aceptación y login correctos. Token inválido → 400;
  aceptación → 200; reutilización → 409. Reserva nueva → 201; cancelación → 200;
  ambos correos recibidos. La prueba conserva el usuario y registros ficticios.
- Se corrigió el cliente de destinatarios para usar Identity en lugar de Company.
  Un usuario existente desuscrito no recibió correos; no se cambió su preferencia.
- Ambas colas de correo quedaron con 0 mensajes pendientes y 0 en vuelo.
  Se comprobaron 16 eventos pendientes en la FIFO de auditoría. No se inició el
  consumidor Audit/SQL Server en esta ejecución: su consumo sigue sin validar aquí.
- El arranque recrea sólo aplicaciones, no bases, para aplicar imágenes actuales.
  Se verificó la coincidencia entre imágenes construidas y contenedores de las
  cinco aplicaciones backend/worker. Los resets transitorios de conexión se reintentan.
- Regresión: 6 tests del arranque, 18 del worker y 98 .NET aprobados. La suite del
  worker también pasó con su imagen Podman; `npm audit --omit=dev` en esa imagen:
  0 hallazgos conocidos. Continúan las advertencias MySqlConnector indicadas abajo.
- `.env` mantiene el hash original. Revisión de 646 archivos publicables sin
  valores de sus secretos; Gitleaks sin hallazgos. `git diff --check` limpio.
- No hubo automatización del navegador ni envío a Internet. LocalStack no persiste
  eventos al recrearse; Mailpit conserva los correos. Sin commit ni push.

## Pendientes de seguridad

- MySqlConnector: advertencias NU1902 ya documentadas en el diagnóstico inicial.
- **Worker revisado posteriormente:** la auditoría inicial informó 23
  vulnerabilidades, incluidas 2 críticas. Se actualizaron las dependencias y se
  adaptó la integración Brevo; la auditoría actual de producción informa **0
  hallazgos conocidos**, también dentro de la imagen reconstruida. Pasaron 18 tests.
  Ver [revisión de seguridad](notification-service/SECURITY_REVIEW.md).
- No se utilizó `npm audit fix --force`. El worker se activa de forma opcional con
  `--messaging`, usando LocalStack y Mailpit. El informe inicial se conserva como referencia histórica
  en `/tmp/opencode/workly-containers/notification-audit.json`.
