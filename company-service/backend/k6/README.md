# Cómo correr las pruebas k6

1. Levantar el backend con Docker Compose.

2. Entrar a la carpeta de pruebas:

```bash
cd k6
```

3. Crear un archivo `.env` con los datos básicos:

```env
BASE_URL=http://localhost:5000
ADMIN_EMAIL=admin@saas.com
ADMIN_PASSWORD=Password1
MEMBER_EMAIL=usuario@saas.com
MEMBER_PASSWORD=Password1
API_KEY_HEADER_NAME=X-API-KEY
```

4. Correr una prueba individual:

```bash
./run.sh tests/load/auth/auth-login.load.js
```

Ejemplo:

```bash
./run.sh tests/load/users/users-get-all.load.js
```

5. Correr todas las pruebas configuradas:

```bash
./run-all-load.sh
```

6. Correr todos los spike tests:

```bash
./run-all-spike.sh
```

7. Cambiar duración o carga para tests `load`, si se necesita:

```bash
RATE=20 TEST_DURATION=1m ./run.sh tests/load/auth/auth-login.load.js
```

8. Correr un spike test individual:

```bash
./run.sh tests/spike/health/health-check.spike.js
```

9. Ajustar el perfil de spike con variables de entorno:

```bash
SPIKE_START_RATE=10 \
SPIKE_RATE=180 \
SPIKE_RAMP_UP=15s \
SPIKE_HOLD=45s \
SPIKE_RAMP_DOWN=20s \
SPIKE_COOLDOWN=30s \
./run.sh tests/spike/rooms/rooms-top.spike.js
```

Los spike tests agregados cubren:

- `/health`
- `/api/rooms/top` (RF8), mantenido en el backend principal como endpoint compatible que actua como proxy hacia el `reservation-service`

Las pruebas de endpoints `/api/reservations/*` pertenecen al `reservation-service`, que es el dueño actual de reservas. Este backend principal conserva usuarios, rooms, companies, areas y API Keys; no persiste reservas.

Los resultados se guardan en la carpeta `results/`.
