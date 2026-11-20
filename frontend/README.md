# Workly Frontend

Workly Frontend es la aplicacion web de la plataforma Workly. Esta capa provee la interfaz visual para que administradores y miembros interactuen con la plataforma de gestion de espacios de trabajo, reservas y credenciales de acceso.

La aplicacion esta desarrollada en Angular y consume la API REST del backend mediante una URL base configurable por ambiente. Su objetivo es ofrecer una experiencia de usuario clara, consistente y separada de la logica de negocio del servidor.

## Proposito

El proposito de este proyecto es brindar la capa de presentacion de Workly para que los usuarios puedan operar la plataforma desde el navegador. En concreto, permite:

- iniciar sesion y mantener una sesion autenticada segun rol;
- administrar empresas y usuarios invitados desde el perfil administrador;
- consultar el tablero de actividad desde el perfil miembro;
- gestionar reservas, espacios y API Keys desde la interfaz web;
- consumir servicios del backend a traves de una configuracion de entorno local.

La aplicacion no implementa la logica de negocio principal. Su responsabilidad es mostrar informacion, capturar acciones del usuario y delegar las operaciones al backend.

## Alcance

Se incluyen las pantallas y flujos necesarios para el uso operativo de Workly en el navegador. El frontend cubre:

- autenticacion de usuarios;
- redireccion segun sesion y rol;
- navegacion privada para administradores y miembros;
- visualizacion y gestion de entidades expuestas por la API;
- manejo de estados vacios, error 404 y acceso no autorizado;
- configuracion del ambiente local mediante archivo `.env`.

## Interfaces Implementadas

### Publicas

- **Home redirect**: redirecciona la raiz de la aplicacion segun la sesion del usuario.
- **Login**: formulario de acceso al sistema.
- **Flujo de aceptacion de invitacion**: pantalla para completar el alta desde el enlace enviado por correo.
- **Not found**: pagina para rutas inexistentes.

### Administrador

La navegacion administrativa se monta sobre un layout propio y requiere autenticacion con rol `Admin`.

- **Empresas** (`/admin`): visualizacion y gestion de empresas registradas.
- **Usuarios / Invitaciones** (`/admin/invitations`): alta y administracion de invitaciones para nuevos usuarios.

### Miembro

La navegacion de miembros se monta sobre un layout propio y requiere autenticacion con rol `Member`.

- **Dashboard** (`/app/dashboard`): resumen de actividad y reservas.
- **Reservas** (`/app/reservations`): consulta y gestion de reservas.
- **API Keys** (`/app/api-keys`): administracion de claves de acceso a la API.
- **Espacios** (`/app/spaces`): exploracion y gestion de espacios disponibles.


## Requisitos

Para ejecutar el proyecto se requiere:

- Node.js 20 o superior.
- npm 11 o compatible con la version declarada en el proyecto.
- Acceso al backend de Workly para que las pantallas consuman datos reales.

## Configuracion de un nuevo ambiente de desarrollo

### 1. Ubicarse en el frontend

Desde la raíz del monorepo, ubicarse en la aplicación Angular.

```powershell
cd frontend/workly-front
```

### 2. Instalar dependencias

```powershell
npm install
```

### 3. Crear el archivo de entorno local

La aplicacion lee las variables `API_URL` y `RESERVATION_API_URL` desde un archivo `.env` ubicado en la raiz de `workly-front`.

Si no existe el archivo, se utiliza por defecto:

```env
API_URL=http://localhost:5000
RESERVATION_API_URL=http://localhost:8081
```

Esos valores se inyectan en tiempo de desarrollo dentro de `env.js`, que luego es cargado desde `src/index.html`.

### 4. Levantar el frontend en desarrollo

```powershell
npm run start
```

Este comando:

- genera `public/env.js` y `src/assets/env.js` a partir del `.env`;
- levanta Angular en modo desarrollo;
- observa cambios en `.env` para regenerar la configuracion automaticamente.

En desarrollo, la aplicacion normalmente queda disponible en la URL que muestra Angular CLI.

## Scripts Disponibles

- `npm run start`: genera la configuracion de entorno y ejecuta `ng serve`.
- `npm run build`: genera `env.js` y compila la aplicacion para produccion.
- `npm run watch`: compila en modo desarrollo con observacion de cambios.
- `npm run generate-env`: genera manualmente los archivos `env.js`.

## Configuracion de Entorno

El frontend resuelve la URL base del backend principal desde `globalThis.__WORKLY_API__.baseUrl` y la URL del reservation-service desde `globalThis.__WORKLY_API__.reservationBaseUrl`. Esas variables se definen mediante el archivo generado `env.js`.

Orden de prioridad:

1. Valores definidos en `.env` como `API_URL` y `RESERVATION_API_URL`.
2. Valor por defecto `http://localhost:5000` para el backend principal.
3. Fallback de `RESERVATION_API_URL` a `API_URL` si no se define.

Los archivos generados se escriben en:

- `workly-front/public/env.js`
- `workly-front/src/assets/env.js`

## Despliegue con Docker

El repositorio incluye un `docker-compose.yml` para construir y ejecutar la imagen del frontend.

```powershell
docker compose up --build
```

La aplicacion queda publicada en `http://localhost:8080`.
