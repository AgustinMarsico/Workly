# Configuración local de Workly

## Archivos

- `.env`: configuración privada, ya generada para este equipo con permisos `600`.
- `.env.example`: plantilla compartible, sin secretos.
- `.gitignore`: excluye `.env` y variantes privadas, incluso en subdirectorios.
- `compose.yaml`: Compose central para Podman; no usa los Compose individuales.
- `compose.messaging.yaml`: extensión opcional con LocalStack, Mailpit y el worker.
- `scripts/init-local-env.py`: genera un entorno nuevo, sin imprimir secretos ni
  sobrescribir un `.env` existente.

Para preparar otro entorno, desde `workly/`:

```bash
python3 scripts/init-local-env.py
podman compose --env-file .env -f compose.yaml config -q
```

El segundo comando sólo valida la configuración; no construye ni arranca servicios.
No ejecutar `config` sin `-q` al compartir una terminal o guardar logs: la salida
completa contiene los secretos interpolados.

## Qué recibe cada componente

| Componente | Secretos recibidos |
| --- | --- |
| Company | JWT, clave interna y contraseña de su usuario MySQL |
| Identity | JWT, clave interna, secreto de API keys y contraseña de su usuario MySQL |
| Rooms | JWT y contraseña de su usuario MySQL |
| Reservation | JWT, clave interna y contraseña de su usuario MySQL |
| Audit API | JWT y contraseña del usuario SQL restringido |
| Audit migrate | JWT, contraseña de administración SQL y contraseña del usuario que debe crear |
| Bases MySQL | Sólo su contraseña root y la de su usuario de aplicación |
| SQL Server | Sólo su contraseña de administración |
| Frontend | Ningún secreto; sólo URLs públicas como argumentos de build |
| Worker local / LocalStack | Sólo credenciales AWS ficticias `test`; SMTP sin autenticación |

Los procesos de migración MySQL usan el usuario de su propia base. Los servicios
de negocio no reciben las contraseñas root. El `.env` completo no se monta ni se
inyecta mediante `env_file`, y queda fuera de los contextos de construcción.
Los secretos siguen siendo variables de entorno: el usuario que administra Podman
puede verlos mediante `inspect`. Esto no sustituye un gestor de secretos.

## URLs y puertos

Las APIs se comunican por DNS en la red de Compose, siempre por su puerto interno
`8080`. No dependen de puertos del host ni de `host.docker.internal`.

| Servicio | URL pública predeterminada |
| --- | --- |
| Frontend | http://localhost:8080 |
| Company | http://localhost:5000 |
| Identity | http://localhost:5100 |
| Rooms | http://localhost:5300 |
| Reservation | http://localhost:8081 |
| Audit (opcional) | http://localhost:5151 |
| Mailpit (con `--messaging`) | http://localhost:8025 |
| LocalStack (con `--messaging`) | http://localhost:4566 |

Los puertos se publican exclusivamente en `127.0.0.1`, no en toda la red.
Las bases no publican puertos al host. Para cambiar un puerto de API, actualizar
tanto `*_PORT` como `PUBLIC_*_API_URL` en `.env` y reconstruir el frontend.
Las URLs del navegador nunca deben contener nombres como `identity-api`.

## Modo local inicial

- Auditoría asíncrona desactivada con `SKIP_AUDIT_QUEUE=true`.
- Notificaciones de invitaciones y reservas desactivadas con los flags `SKIP_*`.
- Sin credenciales AWS, Brevo ni New Relic. Profiling .NET desactivado.
- El Compose pasa `INSTALL_NEW_RELIC=false` durante el build; los Dockerfiles
  conservan la opción de instalarlo fuera de este entorno local.
- Notification no está incluido: sin colas no tiene trabajo válido que procesar.
- Audit y SQL Server son opcionales, bajo el perfil `audit`. La pantalla de
  auditoría no funcionará sin ese perfil; con el perfil sólo se consultan los datos
  locales/seed, no se consumen nuevos eventos mientras SQS esté desactivado.

No cambiar los flags a `false` esperando habilitar la nube: se necesita una
configuración adicional de infraestructura, credenciales y colas/topics.

## Arranque validado con Podman

Las imágenes y migraciones ya fueron verificadas. Se recomienda el script por
etapas, que comprueba los códigos de salida de las migraciones y evita el bloqueo
observado en el arranque conjunto directo de podman-compose 1.6.0:

```bash
# Negocio y frontend, sin SQL Server ni Audit (imágenes ya construidas):
python3 scripts/up-local.py

# Reconstruir primero:
python3 scripts/up-local.py --build

# Incluir la API de auditoría y SQL Server (requiere más memoria):
python3 scripts/up-local.py --audit

# SNS/SQS y correos locales, sin cuentas externas:
python3 scripts/up-local.py --build --messaging

# Detener contenedores conservando los datos:
podman compose --env-file .env -f compose.yaml -f compose.messaging.yaml --profile audit down
```

Las APIs Company, Identity y Rooms esperan la finalización de sus migraciones.
Reservation ejecuta su migración durante el arranque. La configuración ya se
validó y se probó con bases reales. Audit se probó por separado para reducir
consumo de memoria. No ejecutar simultáneamente los Compose individuales.
Resultados y límites de las pruebas: `VALIDACION_CONTENEDORES.md`.

Cada API y su proceso de migración comparten la misma imagen local
(`localhost/workly-company:local`, por ejemplo), evitando construir versiones
distintas de un mismo servicio. Los contextos de build excluyen `.env`, `bin`,
`obj` y otros artefactos locales. El frontend se construye con Node 22 y npm 11.6.2.

## Mensajería local opcional

`python3 scripts/up-local.py --build --messaging` añade LocalStack **4.14.0**,
Mailpit **1.34.0** y el worker. No requiere token de LocalStack, credenciales AWS
reales, Brevo ni socket Docker. El overlay habilita las notificaciones y la
publicación de auditoría sin modificar los flags privados de `.env`.

El script espera la inicialización de dos topics, tres colas y dos suscripciones,
y después la salud del worker, antes de arrancar las APIs. Recrea las aplicaciones
para aplicar las imágenes y configuración actuales; no recrea los datos.

- Mailpit: http://localhost:8025. Captura todo el SMTP, sin enviar a Internet.
- LocalStack: http://localhost:4566. Credenciales ficticias `test`, cuenta local
  `000000000000`, región `LOCAL_AWS_REGION` (por defecto `us-east-1`).
- Variables opcionales: `LOCALSTACK_PORT`, `MAILPIT_PORT`, `LOCAL_AWS_REGION`.
- Prueba repetible: `python3 scripts/check-local-invitation.py --reservations`.
  Crea un usuario ficticio, acepta su invitación, comprueba el login y crea/cancela
  una reserva nueva. Conserva esos registros y los correos; no altera reservas previas.
  Usa el administrador demo, o `WORKLY_TEST_ADMIN_EMAIL` / `WORKLY_TEST_ADMIN_PASSWORD`.
- Los usuarios desuscritos no reciben avisos de reservas; no se cambian sus preferencias.
- Los eventos de auditoría quedan en la cola FIFO hasta iniciar el consumidor con
  `python3 scripts/up-local.py --messaging --audit` (SQL Server necesita más memoria).
  La construcción de Audit se verificó; su consumo integrado no se probó en esta ejecución.
- LocalStack usa `PERSISTENCE=0`: al recrearlo se pierden los eventos pendientes y
  se inicializan nuevamente los recursos. Mailpit conserva correos en `mailpit-data`.
- Para volver al modo sin notificaciones: `python3 scripts/up-local.py`.
  Esto desactiva los publicadores; los emuladores ya arrancados pueden seguir vivos.

La prueba comprueba APIs y el enlace del correo, no automatiza el navegador. Los
correos guardados contienen enlaces privados: no compartir su contenido ni URLs.

## Cuidado de los secretos y datos

### Reiniciar todas las bases a migraciones y seeds

Es posible recrear sólo los volúmenes de las bases. **Es irreversible sin backup**:
elimina empresas, usuarios, invitaciones, reservas y auditorías creados después de
la inicialización. El arranque aplica migraciones y seeds otra vez: no quedan
tablas vacías, sino los datos demo definidos por cada servicio (incluidas reservas).
Los usuarios demo de Identity se crean en `Development`.

Los siguientes comandos son para el proyecto predeterminado `workly`, desde esta
carpeta. Son instrucciones manuales; no se ejecutan como parte del arranque normal:

```bash
# Primero hacer backup si hay datos que se quieran recuperar.
podman compose --env-file .env -f compose.yaml -f compose.messaging.yaml --profile audit down

# Eliminar únicamente los cinco volúmenes de bases, NO otros volúmenes de Podman:
podman volume rm workly_company-db-data workly_identity-db-data workly_rooms-db-data workly_reservation-db-data workly_audit-db-data

# Recrear las bases de negocio, migraciones, seeds y servicios:
python3 scripts/up-local.py --messaging
```

Si alguno de esos volúmenes no existe, omitir sólo ese nombre; no usar `--force`
si está en uso. Si se cambió `COMPOSE_PROJECT_NAME`, comprobar primero los nombres
reales con `podman volume ls`. Audit se inicializará al arrancar con `--audit`;
se puede combinar con `--messaging` cuando haya memoria suficiente.

Conservar el `.env`: no hace falta regenerar secretos. Cerrar sesión en el navegador
después del reset, porque los JWT anteriores no se invalidan al borrar las bases.
Mailpit conserva los correos antiguos en `workly_mailpit-data`; sus invitaciones
ya no corresponden a las bases nuevas. LocalStack reinicializa sus colas sin los
eventos anteriores. No ejecutar los smoke tests si se quiere mantener sólo los seeds.

- No compartir `.env`, salidas completas de `config`/`inspect` ni contraseñas en logs.
- Guardar una copia segura del `.env` si se quieren conservar sus volúmenes.
- No regenerar contraseñas sin planificar la rotación: las variables de inicialización
  de las bases **no cambian las contraseñas en un volumen ya creado**.
- Cambiar JWT invalida tokens existentes; cambiar el secreto de API keys puede
  invalidar las claves emitidas.
- No usar `down -v` para detener el sistema: elimina los volúmenes y sus datos.
- Esta configuración y los usuarios seed son exclusivamente para desarrollo.
