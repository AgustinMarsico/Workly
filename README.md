# Workly

Workly es una plataforma SaaS para gestionar espacios de trabajo y reservas de
distintas empresas. Permite organizar áreas y usuarios, administrar salas,
escritorios y auditorios, reservarlos y consultar la actividad del sistema.

El proyecto evolucionó desde un monolito hacia una arquitectura de microservicios.
Cada servicio administra sus propios datos y se comunica con los demás mediante
HTTP o eventos. El frontend consume las APIs directamente.

## Componentes

### Company Service

Administra las empresas cliente, sus datos y sus áreas internas. Es la referencia
organizacional para los demás servicios; no almacena usuarios, espacios ni reservas.

- **Tecnologías:** C#, .NET 8, Entity Framework Core y MySQL.
- **API local:** http://localhost:5000
- **Carpeta:** [company-service](company-service/)

### Identity Service

Gestiona usuarios, roles, contraseñas, login, invitaciones y API keys. Emite los
tokens JWT utilizados por las APIs y mantiene las asociaciones de cada usuario
con su empresa y área.

- **Tecnologías:** C#, .NET 8, Entity Framework Core y MySQL.
- **API local:** http://localhost:5100
- **Carpeta:** [identity-service](identity-service/)

### Rooms Service

Administra los espacios físicos disponibles: salas, escritorios y auditorios,
incluyendo capacidad y equipamiento. Se integra con Identity para autenticación
y con Reservation para información relacionada con las reservas.

- **Tecnologías:** C#, .NET 8, Entity Framework Core y MySQL.
- **API local:** http://localhost:5300
- **Carpeta:** [rooms-service](rooms-service/)

### Reservation Service

Gestiona la creación, modificación y cancelación de reservas, evitando
superposiciones. Expone consultas de historial, próximas reservas, búsquedas y
estadísticas. Consulta usuarios y espacios por HTTP y puede publicar eventos de
notificación y auditoría.

- **Tecnologías:** C#, .NET 8, Entity Framework Core y MySQL.
- **API local:** http://localhost:8081
- **Carpeta:** [reservation-service](reservation-service/)

### Audit Service

Consume eventos de acciones relevantes mediante SQS y guarda registros de
auditoría inmutables: permite insertar y consultar, pero no modificar ni eliminar.
Expone una API de consulta para administradores.

- **Tecnologías:** C#, .NET 8, Entity Framework Core, SQL Server y AWS SQS.
- **API local opcional:** http://localhost:5151
- **Carpeta:** [audit-service](audit-service/)

### Notification Service

Worker que consume mensajes de invitaciones y cambios de reservas para enviar
correos electrónicos. Utiliza dos colas SQS alimentadas por topics SNS. No expone
una API de negocio ni mantiene una base de datos propia.

- **Tecnologías:** Node.js, AWS SNS/SQS y Brevo; también admite SMTP o modo log.
- **Ejecución local inicial:** desactivado, sin envío de correos.
- **Carpeta:** [notification-service](notification-service/)

### Frontend

Aplicación web para administradores y miembros. Incluye login, gestión de empresas
e invitaciones, exploración de espacios, reservas, API keys y consulta de auditoría.
Se construye con Angular y se sirve mediante Nginx en el entorno de contenedores.

- **Tecnologías:** Angular 21, TypeScript y Nginx.
- **Acceso local:** http://localhost:8080
- **Carpeta:** [frontend](frontend/)

## Ejecutar en local

Se necesita **Podman**, un proveedor de Compose como **podman-compose** y
**Python 3**. La compilación se realiza dentro de contenedores: no requiere .NET
ni Node instalados en el host.

Desde esta carpeta:

```bash
# Sólo si todavía no existe .env: genera secretos exclusivos de desarrollo.
python3 scripts/init-local-env.py

# Construir y arrancar bases, migraciones, APIs y frontend.
python3 scripts/up-local.py --build

# En siguientes arranques, reutilizando las imágenes:
python3 scripts/up-local.py
```

El script comprueba las migraciones y la disponibilidad de las APIs antes de
anunciar que el sistema está listo. No sobrescribe el `.env` existente.

**Usuarios iniciales de desarrollo** (contraseña de demostración `Password1`):

| Rol | Email |
| --- | --- |
| Administrador | `admin@saas.com` |
| Miembro | `usuario@saas.com` |

Estas cuentas se crean únicamente cuando Identity ejecuta sus migraciones en
`Development`; no son credenciales privadas ni deben habilitarse en producción.

Para incluir Audit y SQL Server, con mayor consumo de memoria:

```bash
python3 scripts/up-local.py --build --audit
```

En el modo local inicial, AWS, la auditoría asíncrona y los correos están
desactivados. El perfil Audit permite consultar sus datos iniciales, pero no
recibe nuevos eventos sin configurar SQS. Notification no forma parte de este
arranque; sus dependencias requieren revisión de seguridad antes de habilitarlo.

Para detener el entorno **conservando los datos**:

```bash
podman compose --env-file .env -f compose.yaml --profile audit down
```

No ejecutar los Compose individuales junto con el central. No compartir `.env`
ni utilizar los usuarios iniciales en un despliegue público.

## Documentación

- [Configuración local](CONFIGURACION_LOCAL.md): variables, secretos, puertos y manejo de datos.
- [Validación de contenedores](VALIDACION_CONTENEDORES.md): pruebas realizadas y pendientes de seguridad.
- [Diagnóstico](DIAGNOSTICO.md): hallazgos iniciales y evolución de las correcciones.

- [Auditoría de publicación](AUDITORIA_PUBLICACION.md): saneamiento y verificaciones previas a Git.

## Licencia

El código de Workly se distribuye bajo la [licencia MIT](LICENSE).
Las dependencias de terceros conservan sus propias licencias.
