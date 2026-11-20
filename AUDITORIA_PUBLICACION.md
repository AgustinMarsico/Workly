# Preparación para publicar Workly

## Alcance

El monorepo es exclusivamente esta carpeta `workly/`. Se inicializó Git y se
preparó y verificó el índice; no se hicieron commits ni publicaciones. Los archivos
de la carpeta superior y el historial de otros repositorios no forman parte del
candidato y no se modificaron.

## Saneamiento aplicado

- Se eliminaron los destinatarios personales y la cuenta AWS fija del script
  `notification-service/scripts/publish-test-message.sh`. Ahora exige configuración
  externa y no imprime los destinatarios.
- Se retiraron JWT, secretos de API keys y conexiones con contraseñas de los
  `appsettings` versionables. Las plantillas dejan vacíos los valores privados.
- Los Compose individuales ya no proporcionan contraseñas o secretos predeterminados:
  rechazan el arranque si no se suministran en un entorno privado.
- El Compose central conserva la configuración del `.env` local, sin divulgarlo.
- Identity sólo crea sus usuarios de demostración en `Development`. Las contraseñas
  de tests/fixtures y los usuarios de ejemplo documentados no son secretos reales.
- Las carpetas se llaman `audit-service`, `company-service`, `frontend`,
  `identity-service`, `notification-service`, `reservation-service` y `rooms-service`.
  Se actualizaron Compose, documentación, enlaces y parámetros de scripts.
- Los scripts de despliegue no tienen un repositorio personal predeterminado.
  El repositorio destino se suministra con `WORKLY_GITHUB_REPOSITORY` o `-Repository`.
- Los datos de empresa iniciales son ficticios. No se modificaron los registros
  que ya existían en las bases locales.
- Se ampliaron las exclusiones de Git para bytecode Python, `.run/`, entornos
  privados, resultados de carga, credenciales, certificados y dumps comunes.
  No se eliminaron migraciones ni fixtures SQL legítimos.
- Se conservaron lockfiles, licencias y atribuciones de dependencias externas.

## Datos que permanecen privados

El `.env` local no se regenera ni se elimina: contiene los secretos necesarios
para acceder a los volúmenes existentes. Tiene permisos `600` y no debe subirse
con `git add -f`. `.env.example` es la plantilla publicable, sin esos valores.

Los contenedores, volúmenes, informes temporales y artefactos ignorados no forman
parte de la publicación. Algunos artefactos antiguos pueden conservar rutas de
compilación o configuraciones anteriores; no deben exportarse junto con el código.

## Verificación posterior

Se repitió el escaneo local con Gitleaks 8.30.1 y búsquedas contextuales sobre el
conjunto publicable. Después se verificaron también los blobs exactos del índice
Git, no sólo los archivos del árbol de trabajo.

| Comprobación | Resultado |
| --- | --- |
| Archivos candidatos a publicación | 638 |
| Gitleaks sobre esos candidatos | 0 hallazgos |
| Archivos preparados en el índice Git | 638 |
| Gitleaks y revisión contextual del índice | 0 hallazgos |
| Correos, cuenta AWS, identificadores y claves retirados | 0 coincidencias en candidatos |
| Secretos locales actuales | 13, únicamente en `.env` |
| Contenido y permisos de `.env` | Sin cambios; permisos `600`; ignorado por Git |
| Bytecode, resultados de carga y salidas privadas | Excluidos |
| Compose central y Compose individuales revisados | Configuración válida |
| Configuración JSON y script de notificaciones | Sintaxis válida; publicación probada con AWS simulado |
| Arranque principal reconstruido | Migraciones correctas y APIs/frontend con HTTP 200 |
| Login local y enlaces del README | Verificados |
| Tests Rooms y Audit | 98 aprobados |
| Tests del script de arranque | 2 aprobados |

Los informes técnicos redactados están fuera del árbol publicable, en
`/tmp/opencode/workly-sanitization/`. Los resultados corresponden al contenido
actual. La verificación adicional del índice se registra en
`/tmp/opencode/workly-staged-audit/`.

## Antes del commit

El contenido exacto del índice ya fue revisado y escaneado. El siguiente paso,
únicamente cuando se autorice, es crear el commit. No hay remoto configurado.
No se puede garantizar riesgo cero a partir de un análisis automático.
Si una credencial se hubiera publicado anteriormente, debe rotarse: borrarla o
ignorarla no la revoca. No se validaron credenciales contra proveedores externos.

Las vulnerabilidades de dependencias documentadas en `VALIDACION_CONTENEDORES.md`
son un asunto separado del filtrado de datos: publicar código saneado no implica
que sea seguro desplegarlo públicamente sin revisar esas alertas.
