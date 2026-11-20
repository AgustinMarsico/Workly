# Configuración local de Workly

## Archivos

- `.env`: configuración privada, ya generada para este equipo con permisos `600`.
- `.env.example`: plantilla compartible, sin secretos.
- `.gitignore`: excluye `.env` y variantes privadas, incluso en subdirectorios.
- `compose.yaml`: Compose central para Podman; no usa los Compose individuales.
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

# Detener contenedores conservando los datos:
podman compose --env-file .env -f compose.yaml --profile audit down
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

## Cuidado de los secretos y datos

- No compartir `.env`, salidas completas de `config`/`inspect` ni contraseñas en logs.
- Guardar una copia segura del `.env` si se quieren conservar sus volúmenes.
- No regenerar contraseñas sin planificar la rotación: las variables de inicialización
  de las bases **no cambian las contraseñas en un volumen ya creado**.
- Cambiar JWT invalida tokens existentes; cambiar el secreto de API keys puede
  invalidar las claves emitidas.
- No usar `down -v` para detener el sistema: elimina los volúmenes y sus datos.
- Esta configuración y los usuarios seed son exclusivamente para desarrollo.
