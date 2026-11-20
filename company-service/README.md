# Workly Backend

Workly es una aplicación SaaS orientada a la gestión de espacios físicos y reservas en entornos de trabajo híbrido. Permite a las empresas administrar sus recursos (salas, escritorios, auditorios) y a los usuarios reservarlos de forma eficiente, evitando conflictos de disponibilidad y mejorando la organización del espacio de trabajo.

El sistema está diseñado bajo una arquitectura cloud-native, priorizando la escalabilidad, seguridad y observabilidad, y se expone mediante una API REST que puede ser consumida tanto por interfaces web como por sistemas externos.

## Proposito

Workly Backend concentra la logica de negocio del backend principal que necesitan los clientes de Workly para operar sobre la plataforma. Las reservas fueron migradas a un microservicio separado (`reservation-service`), que es el dueño de la persistencia y funcionalidad de reservas. El propósito de este proyecto es construir una solución funcional que permita:

- Gestionar empresas dentro del sistema SaaS
- Administrar usuarios mediante invitaciones
- Autenticar usuarios con control de acceso por roles
- Gestionar espacios físicos disponibles
- Integrarse con el microservicio de reservas para permitir reservas evitando superposiciones
- Exponer endpoints REST protegidos mediante API Keys
- Proveer visibilidad del sistema mediante métricas y observabilidad

La solucion esta pensada para mantener separadas las responsabilidades tecnicas. La API no implementa directamente toda la logica en los controladores, sino que delega en servicios, capa de dominio y acceso a datos para facilitar mantenimiento, pruebas y evolucion del proyecto.

## Alcance

Se incluyen las funcionalidades necesarias para gestionar el ciclo completo de uso del sistema, desde la creación de empresas y la incorporación de usuarios mediante invitaciones, hasta la administración de espacios físicos. Las reservas no se persisten en este backend: pertenecen al `reservation-service`, que consulta por HTTP los datos externos que necesita del backend principal. Asimismo, se provee una API REST que permite la integración con sistemas externos mediante el uso de API Keys, facilitando el acceso programático a la información.

- Registro de empresas clientes
- Invitación de usuarios mediante correo electrónico
- Autenticación de usuarios mediante email y contraseña
- Control de acceso basado en roles (administrador y miembro)
- Generación y gestión de API Keys
- CRUD de espacios físicos (salas, escritorios, auditorios)
- Validación de API Keys mediante `GET /api/api-keys/validate` para integraciones internas y externas
- Integración HTTP con el `reservation-service`, dueño de creación, consulta, cancelación lógica, historial, próximas reservas, estadísticas, búsqueda avanzada y top rooms
- Endpoint compatible `GET /api/rooms/top`, mantenido en el backend principal como proxy hacia el `reservation-service`

## Arquitectura de reservas

El backend principal ya no es dueño de reservas. Usuarios, espacios (`rooms`), empresas, áreas y API Keys siguen perteneciendo a este repositorio y a su base de datos principal. El `reservation-service` es el único dueño de la tabla `reservations` y de los endpoints `/api/reservations/*`.

El microservicio de reservas persiste solamente:

- `__EFMigrationsHistory`
- `reservations`

Para validar usuarios, espacios, empresas o API Keys, el `reservation-service` consulta al backend principal por HTTP. En particular, `GET /api/api-keys/validate` pertenece al backend principal.

La funcionalidad de reservas no se describe como CRUD completo porque no existe `PUT` ni `PATCH` para actualizar una reserva. Las operaciones soportadas por el `reservation-service` son creación, consulta, cancelación lógica, historial, próximas reservas, estadísticas, búsqueda avanzada y ranking de espacios más reservados.

## Requisitos

Para ejecutar el proyecto se requiere:
- .NET SDK 8
- Docker Desktop o una instancia local de MySQL 8
- Acceso a un servidor SMTP (opcional, para pruebas de envío de correos)

## Configuracion de un nuevo ambiente

### 1. Ubicarse en el servicio

Desde la raíz del monorepo:

```powershell
cd company-service
```

### 2. Crear el archivo de entorno local

La API carga variables desde `backend/.env` cuando se ejecuta desde `backend/workly-api`.

1. Copiar `backend/.env.example` a `backend/.env`.
2. Completar los valores según el entorno local.

Variables de entorno:

- `SUPER_SECRET_KEY`: Clave secreta utilizada para la generación y validación de tokens JWT, así como para otros mecanismos de seguridad como API Keys. Debe ser un valor seguro y no compartido.
- `ConnectionStrings__DefaultConnection`: Cadena de conexión base a la base de datos MySQL. Define el host, puerto, nombre de la base de datos y usuario. Puede ser complementada o modificada mediante otras variables como DB_PASSWORD.
- `DB_PASSWORD`: Permite sobreescribir la contraseña definida en la cadena de conexión. Es útil para evitar exponer credenciales sensibles directamente en la configuración principal.
- `NEW_RELIC_LICENSE_KEY`: Clave de licencia utilizada por el agente de New Relic para enviar métricas de la aplicación. Permite habilitar el monitoreo de performance, errores y uso del sistema.
- `SQS_AUDIT_QUEUE_URL`: URL de la cola SQS donde el backend publica eventos de auditoría para que los consuma `Audit-Service`.
- `SKIP_AUDIT_QUEUE`: Si vale `true`, desactiva la publicación de eventos de auditoría. Usar solo para pruebas locales.
- `SNS_INVITATION_TOPIC_ARN`: ARN del topic SNS de invitaciones. El notification-service consume estos mensajes y envía el email vía Brevo.
- `SKIP_INVITATION_NOTIFICATION`: Si vale `true`, desactiva la publicación de invitaciones a SNS. Usar solo para pruebas locales.
- `GET /api/users?areaId={areaId}`: Devuelve los usuarios de un area (proxy al identity service). Lo consume reservation-service para notificaciones.

### 3. Crear los archivos `appsettings` locales

Dentro de `backend/workly-api`, se deben copiar las plantillas de configuración:

```powershell
Copy-Item appsettings.example.json appsettings.json
Copy-Item appsettings.Development.example.json appsettings.Development.json
```
Luego, se pueden ajustar los valores según las necesidades del entorno.

### 4. Levantar la base de datos

Se recomienda utilizar Docker para levantar la base de datos:

```powershell
cd backend
docker compose up -d db
```

Eso expone MySQL en `localhost:3306` con la base de datos `worklydb`.

### 5. Ejecutar migraciones

Desde la carpeta `backend/DataAccess`, se deben ejecutar las migraciones:

```powershell
dotnet run -- migrate
```

Este proceso crea la estructura inicial de la base de datos utilizando la configuración definida en las variables de entorno.

### 6. Levantar la API

Desde `backend/workly-api`, se puede levantar la aplicación:

```powershell
dotnet run --environment Development
```

En desarrollo, la API suele quedar disponible en `http://localhost:5000` cuando se usa Docker, o en el puerto que muestre la consola al ejecutar `dotnet run`.

## Opcion con Docker

Como alternativa, es posible ejecutar todo el sistema utilizando Docker Compose: `backend/docker-compose.yml`.

```powershell
cd backend
docker compose up --build
```

Este proceso levanta los siguientes servicios:

- `db`: base de datos MySQL.
- `migrate`: servicio encargado de ejecutar las migraciones.
- `api`: backend expuesto en `http://localhost:5000`.

### Levantar este microservicio con docker-compose

Desde la raiz de este repositorio (`company-service`), el microservicio se levanta usando el compose incluido en `backend/docker-compose.yml`:

```powershell
cd backend
docker compose up --build
```

El comando construye la imagen de la API, levanta MySQL, ejecuta las migraciones y deja el servicio disponible en:

```text
http://localhost:5000
```

Para validar que quedo activo:

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:5000/health
```

Para detenerlo:

```powershell
docker compose down
```

## Pruebas

Para ejecutar las pruebas automatizadas del proyecto, desde la carpeta `backend` se puede utilizar:

```powershell
dotnet test workly-app.sln
```
