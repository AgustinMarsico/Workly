# Workly Reservation Service

Microservicio responsable de la funcionalidad de reservas de Workly.

En desarrollo local con Docker Compose queda publicado en `http://localhost:8081`, manteniendo el puerto interno del contenedor en `8080`.

## Responsabilidad

El `reservation-service` es el dueño de reservas. El backend principal ya no persiste ni administra directamente reservas; mantiene usuarios, rooms, companies, areas y API Keys.

La base de datos del microservicio debe contener unicamente:

- `__EFMigrationsHistory`
- `reservations`

Todo dato externo a reservas se consulta por HTTP al backend principal. Esto incluye validacion de usuarios, espacios, empresas y API Keys. El endpoint `GET /api/api-keys/validate` pertenece al backend principal.

## Funcionalidad

La funcionalidad de reservas no es un CRUD completo porque no existe `PUT` ni `PATCH /api/reservations/{id}`. Las operaciones soportadas son creacion, consulta, cancelacion logica, historial, proximas reservas, estadisticas, busqueda avanzada y top rooms.

Endpoints principales:

- `POST /api/reservations`
- `GET /api/reservations`
- `GET /api/reservations/{id}`
- `DELETE /api/reservations/{id}` como cancelacion logica
- `PUT /api/reservations/{id}` para modificar una reserva activa
- `GET /api/reservations/upcoming`
- `GET /api/reservations/upcoming/stats`
- `GET /api/reservations/history`
- `GET /api/reservations/search`
- `GET /api/reservations/top-rooms`

## Compatibilidad

El backend principal conserva `GET /api/rooms/top` como endpoint compatible. Ese endpoint funciona como proxy hacia `GET /api/reservations/top-rooms` en este microservicio.

## Notificaciones por email (SNS)

Cuando se crea, modifica o cancela una reserva, el servicio:

1. Obtiene el area del usuario que realizo la accion (identity service).
2. Consulta `GET /api/users?areaId={areaId}` en el backend principal.
3. Si la consulta es exitosa, guarda los destinatarios en `area_notification_recipients` (cache local).
4. Si la consulta falla, usa la cache local como backup.
5. Publica un mensaje en `SNS_RESERVATION_TOPIC_ARN` con los emails del area que tengan `emailSubscribed: true`.

Variables de entorno:

- `SNS_RESERVATION_TOPIC_ARN`: ARN del topic SNS de reservas (`notification-topic` del notification-service).
- `SKIP_RESERVATION_NOTIFICATION`: `true` para desactivar en local.

El backend principal expone `GET /api/users?areaId={areaId}` y reenvia la solicitud al identity service. El identity service debe devolver usuarios con este formato:

```json
[
  {
    "id": "guid",
    "email": "user@example.com",
    "name": "John",
    "areaId": "guid",
    "emailSubscribed": true
  }
]
```

## Levantar con Docker Compose

Este repositorio incluye `docker-compose.yml`, por lo que el microservicio de reservas puede levantarse de forma independiente desde la raiz de `reservation-service`:

```powershell
docker compose up --build
```

El compose levanta:

- `reservation-db`: base MySQL local.
- `reservation-service`: API de reservas.

La API queda publicada en:

```text
http://localhost:8081
```

El contenedor usa internamente el puerto `8080`. Por defecto, para integrarse con otros servicios locales fuera del compose, utiliza:

- `MAIN_SERVICE_BASE_URL=http://host.docker.internal:5000`
- `IDENTITY_SERVICE_BASE_URL=http://host.docker.internal:5100`

Si se quiere cambiar algun valor, crear o editar el archivo `.env` del repositorio antes de levantar el compose.

Health check:

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:8081/health
```

Para detener el servicio:

```powershell
docker compose down
```
