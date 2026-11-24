# Backend TutorFlow

API REST en ASP.NET Core sobre .NET 8. Resuelve el perfil de demostración, valida preguntas y permisos, obtiene información desde la API mock por HTTP y envía a Gemini el contexto permitido junto con `rules.md` y `api.md`.

## Requisitos y configuración

- .NET SDK 8 o superior; Docker compila el backend con SDK `10.0.404`.
- Para ejecutar consultas Gemini, crea `.env` desde `.env.example` en la raíz y configura `GEMINI_API_KEY`. `GEMINI_MODEL` es opcional y por defecto usa `gemini-3.5-flash-lite`.
- `.env` y sus variantes se ignoran por Git. No incluyas credenciales en código, logs o commits.
- `MOCK_API_URL` configura la API académica desde la perspectiva del backend. En Compose su valor predeterminado es `http://mock-api:4000/`; en desarrollo local, `http://localhost:4000/`.

## Ejecutar en local

Primero inicia la API mock desde `initial_docs/mock-api/` con Node 18 o superior:

```powershell
node server.js
```

En otra terminal, desde `backend/TutorFlow.Api/`:

```powershell
dotnet run --urls http://localhost:5000
```

La clave debe estar disponible en el entorno o en el `.env` de la raíz. Para iniciar el conjunto completo en contenedores, consulta [la guía raíz](../README.md#configuración-y-arranque-con-docker).

## Contrato y permisos

Los endpoints disponibles son `GET /api/health`, `GET /api/users` y `POST /api/chat/messages`. No existe login ni token para esta prueba; `userId` selecciona un perfil ficticio. El rol y el ámbito se resuelven en el servidor. Los estudiantes solo acceden a su expediente; los coordinadores, a sus programas. Los detalles de cuerpos, respuestas y errores están en [docs/backend-endpoints.md](../docs/backend-endpoints.md).

El pipeline valida pregunta/perfil, bloquea patrones obvios de prompt injection y aplica el alcance antes de invocar Gemini. La detección por patrones es heurística; esta API no está diseñada para datos reales ni exposición pública.

## Gemini y errores

El SDK `Google.GenAI` se integra detrás de `IAgentClient`. El backend controla los errores comunes: cuota (429), servicio o red (502), configuración/modelo/no disponibilidad (503) y timeout (504). Los fallos transitorios tienen hasta tres intentos en total. Los logs dan detalles técnicos sanitizados y no imprimen la clave.

## Build y pruebas

Desde la raíz:

```powershell
dotnet restore backend/TutorFlow.sln
dotnet build backend/TutorFlow.sln --no-restore
dotnet test backend/TutorFlow.sln
docker compose --profile test run --build --rm docker-e2e
```

Las pruebas .NET locales usan dependencias simuladas y no consumen cuota. Las pruebas optativas contra Gemini requieren `RUN_GEMINI_INTEGRATION_TESTS=true` y una clave válida; pueden consumir cuota. El runner Docker consulta servicios en ejecución, incluidos los endpoints de la API mock y el chat real.

## Documentación relacionada

- [Arquitectura y decisiones](../docs/architecture-decisions.md)
- [Contrato REST del backend](../docs/backend-endpoints.md)
- [Presentación de entrega](../docs/entrega.md)
