# TutorFlow backend

API REST ASP.NET Core .NET 8. Recibe la identidad de prueba seleccionada, resuelve su rol desde el directorio del servidor, aplica validaciones y permisos, carga `rules.md`/`api.md` y despacha el contexto autorizado a Gemini.

## Requisitos y configuración

- .NET SDK 8 o superior (la configuración Docker usa SDK `10.0.404`).
- Copia `.env.example` a `.env` en la raíz y configura `GEMINI_API_KEY`; `GEMINI_MODEL` es opcional (`gemini-3.5-flash-lite` por defecto). `.env` y variantes están ignorados por Git.
- El SDK oficial `Google.GenAI` envía reglas, identidad/rol resuelto, contexto permitido y la pregunta validada. No se registra la clave.
- Sin clave o con el proyecto/modelo inaccesible, el chat responde 503; cuota 429, red/servicio 502 y timeout 504 son respuestas controladas. HTTP 503 de alta demanda se reintenta hasta dos veces con backoff exponencial y se registra con estado, modelo y detalle del proveedor (sin exponer la API key).
- La mock API Node es opcional para datos personalizados. Desde `initial_docs/mock-api/`, inicia `node server.js` (puerto 4000).
- Inicia localmente con `dotnet run --urls http://localhost:5000` desde `backend/TutorFlow.Api/`.

## Diagnóstico de disponibilidad Gemini

En la validación Docker del 24 de noviembre de 2026, el contenedor tenía `GEMINI_API_KEY` presente y `GEMINI_MODEL=gemini-3.8-flash`. Gemini respondió HTTP 503 con el detalle `This model is currently experiencing high demand`; no fue un 429 de cuota ni un fallo de red/TLS. `gemini-3.8-flash` sigue siendo un nombre válido, pero estaba temporalmente saturado. Para esta ejecución se cambió el `.env` local a `gemini-3.5-flash-lite`, modelo estable que respondió HTTP 200 en la prueba real. La plantilla `.env.example` ya usa ese modelo como valor predeterminado.

El SDK estaba configurado con `Attempts=1`, que desactiva los reintentos. Ahora usa hasta tres intentos con backoff exponencial y jitter para errores transitorios. Los logs incluyen código HTTP, modelo y detalle del proveedor con patrones de API key redactados. Consulta la [lista oficial de modelos](https://ai.google.dev/gemini-api/docs/models) y la [guía oficial de reintentos](https://ai.google.dev/gemini-api/docs/troubleshooting) si cambias el modelo.

## Contrato REST

No hay endpoint de login, bearer token ni cookie de sesión. El `userId` identifica un perfil ficticio para evaluación; el rol y los permisos se obtienen en el servidor, nunca de campos enviados por el cliente.

| Método y ruta | Uso |
|---|---|
| `GET /api/health` | Salud y proveedor. |
| `GET /api/users` | Lista los perfiles disponibles con rol y ámbito de programas. |
| `POST /api/chat/messages` | Recibe `{ "userId": "EST-001", "message": "¿Cuál es mi promedio?" }` y responde con `answer`, `sources`, `provider`. |

Respuestas de error: 400 para usuario desconocido/entrada inválida/anti-inyección, 403 para acceso fuera del alcance del rol, 429 para cuota Gemini, 502 para red/servicio y 503 para clave faltante.

Ejemplo:

```bash
curl http://localhost:5000/api/users
curl -X POST http://localhost:5000/api/chat/messages -H 'Content-Type: application/json' -d '{"userId":"EST-001","message":"¿Cuál es la nota mínima para aprobar?"}'
```

## Validaciones y pruebas

El pipeline valida perfil/rol, pregunta (1–2000 caracteres), patrones obvios de prompt injection y alcance de datos antes de despachar al agente. Estudiantes solo consultan su expediente; coordinadores solo consultan estudiantes de sus programas asignados. Estos permisos se aplican localmente y se envían de nuevo como reglas estrictas a Gemini.

```powershell
dotnet restore backend/TutorFlow.sln
dotnet build backend/TutorFlow.sln --no-restore
dotnet test backend/TutorFlow.sln
```

Las pruebas locales usan un adaptador falso y cubren permisos, entrada directa sin sesión, identidad inválida, anti-inyección, cuota y configuración faltante. Las pruebas opt-in con Gemini requieren `RUN_GEMINI_INTEGRATION_TESTS=true` y pueden consumir cuota. El E2E consulta cada caso del JSON de ejemplo mediante el nuevo contrato directo y una mock API Node iniciada temporalmente.

## Decisión de prueba técnica

Enviar el `userId` permite evaluar roles con facilidad desde la interfaz, Postman y scripts. No es autenticación: cualquier consumidor de la API puede escoger un perfil demo. En producción se exigirían OAuth2/OIDC, tokens JWT firmados y expirables con refresh, claims verificados en servidor, rate limits, CORS estricto, HTTPS/TLS, auditoría y gestión centralizada de secretos. Detalle en [arquitectura y decisiones](../docs/architecture-decisions.md).
