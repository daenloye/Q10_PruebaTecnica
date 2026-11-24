# TutorFlow

Asistente académico de prueba técnica. La interfaz Angular/TypeScript y la API ASP.NET Core (.NET 8) permiten elegir un perfil ficticio y consultar reglamento y datos académicos con permisos de estudiante/coordinador aplicados en el backend. El servicio puede consumirse directamente por REST, sin usar la interfaz.

## Arquitectura y decisiones

- Mock API Node en `initial_docs/mock-api/` con programas, estudiantes, coordinadores, notas y asistencia.
- Backend .NET 8, compilado con SDK `10.0.404`, con controles de entrada, anti-inyección, autorización por rol, documentación/contexto y adaptador Gemini.
- Frontend Angular + TypeScript en una vista: selector **Preguntar como**, chat y respuestas. `BackendApiService` concentra la comunicación HTTP.
- `GET /api/users` lista perfiles y roles. `POST /api/chat/messages` acepta el ID y el mensaje, sin login ni token de sesión.
- Suites .NET unitarias y E2E Docker; pruebas opt-in que consultan Gemini.

Consulta [arquitectura y decisiones](docs/architecture-decisions.md) para la justificación de la identidad directa de prueba, límites de permisos y controles que se requieren en producción.

Consulta también el [contrato de endpoints del backend .NET](docs/backend-endpoints.md) para integrar cualquier cliente REST.

### Simplificación de identidad para la prueba

Se elimina el login y el token para agilizar las pruebas funcionales: el selector permite cambiar de rol en un clic y cualquier consumidor REST puede llamar al backend sin pasar por la UI. La API recibe un ID ficticio y resuelve desde sus propios datos el nombre, rol y alcance; no confía en un rol enviado por el cliente. Este ID en texto plano no prueba quién es la persona y solo es apropiado para los datos demo.

En producción se autenticarían usuarios con OAuth2/OIDC y JWT firmados/expirables más refresh tokens, se autorizarían claims verificados en servidor y se agregarían rate limiting por usuario/IP, CORS estricto, HTTPS/TLS, auditoría y gestión centralizada de secretos.

## Configuración y Docker

1. Copia `.env.example` a `.env` (`Copy-Item .env.example .env`) y configura `GEMINI_API_KEY`. `GEMINI_MODEL` es opcional. No subas `.env`.
2. `MOCK_API_URL` configura la URL interna de Node (predeterminada `http://mock-api:4000/`); `MOCK_API_PORT`, `BACKEND_PORT` y `FRONTEND_PORT` controlan puertos publicados (4000, 5000 y 5173 por defecto). `BACKEND_API_URL` configura el backend que Angular usa en runtime (por defecto `http://localhost:5000`); en desarrollo local se puede editar `frontend/public/config.json`.
3. Levanta y comprueba los servicios:

```powershell
docker compose up -d --build
docker compose ps
```

`DOCKER_CA_CERT_PATH` apunta de forma opcional a un certificado raíz PEM para redes con inspección TLS; el valor predeterminado es `.local-certs/empty.pem`. `DOTNET_RUNTIME_IMAGE` y `NODE_BASE_IMAGE` permiten elegir imágenes base locales si el daemon no puede acceder a registries; sus valores normales están en `.env.example`.

URLs: [frontend/chat](http://localhost:5173), [salud backend](http://localhost:5000/api/health), [usuarios](http://localhost:5000/api/users), [salud mock API](http://localhost:4000/salud) y [programas](http://localhost:4000/programas).

## Uso directo de la API

El perfil se envía en cada consulta. No se requiere token ni llamada de login. Ejemplo con PowerShell/curl:

```powershell
curl.exe http://localhost:5000/api/users
curl.exe -X POST http://localhost:5000/api/chat/messages `
  -H "Content-Type: application/json" `
  -d '{"userId":"EST-001","message":"¿Cuál es la nota mínima para aprobar?"}'
```

Para probar un coordinador, usa un ID listado por `GET /api/users`, por ejemplo `COORD-01`. La respuesta exitosa contiene `answer`, `sources` y `provider`. Usuarios desconocidos/entrada inválida se rechazan con 400; las consultas a expedientes fuera del alcance del rol se rechazan con 403. La pregunta se filtra contra inyecciones antes de llamar a Gemini.

### Comandos de validación humana

Con Docker activo y `.env` configurado, estos comandos se pueden ejecutar desde PowerShell en la raíz:

```powershell
docker compose ps
Invoke-RestMethod http://localhost:4000/salud | ConvertTo-Json
Invoke-RestMethod http://localhost:5000/api/health | ConvertTo-Json
Invoke-RestMethod http://localhost:5000/api/users | Select-Object id,name,role | Format-Table
$body = @{ userId = "EST-001"; message = "¿Cuál es la nota mínima para aprobar?" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/chat/messages -ContentType "application/json" -Body $body
docker compose logs --tail 100 backend frontend mock-api
dotnet test backend/TutorFlow.sln
docker compose --profile test build docker-e2e
docker compose --profile test run --rm docker-e2e
```

## Pruebas

```powershell
npm --prefix frontend install
npm --prefix frontend run build
npm --prefix frontend test
dotnet test backend/TutorFlow.sln
docker compose --profile test run --build --rm docker-e2e
```

Las pruebas Angular usan `HttpTestingController` para las peticiones HTTP y verifican selector/envío en el componente. Las pruebas .NET locales usan un adaptador falso y no consumen cuota. El runner Docker valida que el bundle Angular y la configuración runtime se sirven correctamente, además del listado REST, flujo directo por estudiante/coordinador, entrada inválida, anti-inyección, límites entre programas y respuestas reales Gemini. Para las pruebas opt-in .NET con Gemini configura `RUN_GEMINI_INTEGRATION_TESTS=true`; pueden consumir cuota. Para apagar el entorno: `docker compose down`.
