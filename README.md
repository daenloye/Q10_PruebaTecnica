# TutorFlow

Asistente académico de prueba técnica para estudiantes y coordinadores. El frontend React/TypeScript consume una API ASP.NET Core (.NET 8) que autentica con sesión opaca única, limita los datos académicos por rol, carga `rules.md` y `api.md`, consulta la API mock de Node y envía las consultas autorizadas a Google Gemini.

## Resumen del sistema

- API mock Node en `initial_docs/mock-api/`, con healthcheck y datos de programas, coordinadores, estudiantes, notas y asistencia.
- Backend .NET 8 con SDK de compilación .NET `10.0.404`, login de usuario demo, token único en memoria, controles anti-inyección, reglas de permisos y adaptador Gemini.
- Frontend React + TypeScript para seleccionar usuario y conversar con el agente.
- Pruebas locales para autenticación, validación, roles, inyección, cuota y configuración; pruebas opt-in con Gemini y runner E2E para el sistema en Docker.
- Compose define healthchecks y orden de arranque entre los tres servicios principales.

Consulta el [documento de arquitectura y decisiones](docs/architecture-decisions.md) para el flujo de datos, roles, sesión y simplificaciones de seguridad de esta prueba.

## Configuración

Requisitos: Docker Desktop/Engine y Docker Compose v2; una clave de Google Gemini para respuestas reales.

1. Copia `.env.example` como `.env` en la raíz. En PowerShell: `Copy-Item .env.example .env`.
2. Edita `.env` y configura `GEMINI_API_KEY`; opcionalmente selecciona `GEMINI_MODEL` (por defecto `gemini-3.5-flash-lite`). No publiques ni agregues `.env` a Git.
3. Configura la API Node:
   - `MOCK_API_URL` es la URL que el backend usa dentro de la red Compose; por defecto `http://mock-api:4000/`.
   - `MOCK_API_PORT` es el puerto del host para consultar esa API desde el navegador o herramientas locales; por defecto `4000`.
   - Si cambias el puerto interno del servicio Node, sincroniza el puerto en `.env`, `MOCK_API_URL` y `docker-compose.yml`. Cambiar solo `MOCK_API_PORT` modifica el puerto publicado, no el interno.
4. `BACKEND_PORT` y `FRONTEND_PORT` controlan los puertos publicados, con valores predeterminados `5000` y `5173`.

### Docker Desktop detrás de DNS filtrado o inspección TLS

Si Docker no resuelve `mcr.microsoft.com`, NuGet o npm, en Docker Desktop abre **Settings > Network**, configura **Inhibit DNS resolution** como **No filtering**, aplica y reinicia Docker Desktop. Si las consultas DNS del daemon siguen fallando, configura un DNS accesible desde la red en Docker Desktop **Settings > Docker Engine** (por ejemplo, `"dns": ["8.8.8.8"]`) y aplica. La configuración del daemon es local al equipo.

Si la red inspecciona TLS con una CA privada, exporta esa CA raíz en formato PEM a un archivo local, ignorado por Git (por ejemplo, `.local-certs/organization-root.crt`) y cambia `DOCKER_CA_CERT_PATH` en `.env` para señalarlo. Compose la monta como secreto de build; los Dockerfiles instalan la CA para verificar TLS durante restore/npm y en el backend en ejecución. Sin inspección TLS, conserva `.local-certs/empty.pem`. No desactives la validación de certificados ni publiques la CA local.

Si las imágenes base ya están descargadas localmente y el daemon aún no alcanza un registry, `DOTNET_RUNTIME_IMAGE` y `NODE_BASE_IMAGE` permiten seleccionar esas imágenes sin cambiar Dockerfiles; sus valores normales son `mcr.microsoft.com/dotnet/aspnet:8.0` y `node:20-alpine`. Por ejemplo, se puede usar localmente una imagen SDK .NET 10 como runtime con `DOTNET_ROLL_FORWARD=Major`; la aplicación sigue compilándose para .NET 8.

Compose lee el `.env` raíz y pasa la URL de Node al backend como `MockApi__BaseUrl`. El frontend llama al puerto publicado del backend. `.env` está ignorado por Git y excluido de los contextos Docker; `.env.example` es la plantilla segura versionada.

## Ejecución con Docker

Desde la raíz del repositorio:

```powershell
docker compose up -d --build
docker compose ps
docker compose logs -f
```

Cuando los servicios estén `healthy`, abre el chat y elige un usuario de prueba:

- Frontend/chat: <http://localhost:5173>
- Salud del backend: <http://localhost:5000/api/health>
- Usuarios de prueba: <http://localhost:5000/api/auth/users>
- API mock: <http://localhost:4000/salud> y <http://localhost:4000/programas>

Para ejecutar el flujo E2E dentro de la red Docker, con el backend y frontend levantados:

```powershell
docker compose --profile test run --build --rm docker-e2e
```

El runner consulta `MOCK_API_URL` desde `.env`, valida salud de los tres servicios, descubrimiento de perfiles, tokens de sesión, anti-inyección, contexto, acceso estudiante/coordinador y respuestas/fuentes de Gemini. La suite local (sin consumo de cuota) se ejecuta con `dotnet test backend/TutorFlow.sln`. Para activar las pruebas .NET opt-in de Gemini y su E2E con la API mock propia de tests:

```powershell
$env:RUN_GEMINI_INTEGRATION_TESTS = "true"
dotnet test backend/TutorFlow.sln
Remove-Item Env:RUN_GEMINI_INTEGRATION_TESTS
```

Estas consultas reales pueden consumir cuota. Las pruebas locales cubren también errores controlados de cuota 429 y configuración de clave ausente.

Para apagar los servicios: `docker compose down`. Para ver fallos: `docker compose logs backend frontend mock-api`.

## URLs y puertos predeterminados

| Servicio | URL en el host | Puerto interno Docker |
|---|---|---:|
| Frontend React/Vite | `http://localhost:5173` | 5173 |
| Backend ASP.NET Core | `http://localhost:5000` | 8080 |
| API mock Node | `http://localhost:4000` | 4000 |

El perfil de prueba `docker-e2e` es temporal y no reemplaza los contenedores persistentes. Mantén `docker compose up -d` ejecutándose para validación humana.
