# TutorFlow backend

API ASP.NET Core sobre .NET 8. El backend autentica usuarios ficticios, crea una sesión opaca única en memoria, aplica validaciones y permisos por rol, carga `rules.md` y `api.md`, y despacha un payload estructurado mediante `IAgentClient`.

## Requisitos y ejecución

- .NET SDK 8 o superior.
- Copia `.env.example` a `.env` en la raíz y define `GEMINI_API_KEY`; `GEMINI_MODEL` es opcional y por defecto usa `gemini-3.5-flash-lite`.
- `gemini-3.5-flash-lite` tiene nivel gratuito según la [tabla oficial de precios](https://ai.google.dev/gemini-api/docs/pricing). Los límites de cuota pueden producir respuestas HTTP 429.
- `.env` y sus variantes están ignorados por Git. El backend carga el archivo al iniciar, conserva variables ya definidas en el proceso y no registra la clave.
- `Google.GenAI` es el SDK oficial usado por `GeminiAgentClient`. El payload envía reglas, documentación, contexto académico autorizado e identidad/rol en `systemInstruction`; la pregunta validada se envía como contenido del usuario.
- Sin clave, el chat responde 503; errores de cuota 429, servicio/red 502 y timeout 504 se devuelven con un mensaje controlado.
- El selector de usuario no pide contraseña: identifica perfiles ficticios para recorrer los permisos en la demo y no constituye autenticación real.
- API mock requerida para incluir datos personalizados: Node.js 18+, desde `initial_docs/mock-api/`, `node server.js` (puerto 4000). El chat conserva las consultas normativas si el mock no está disponible.
- Desde `backend/TutorFlow.Api/`: `dotnet run --urls http://localhost:5000`.
- Salud: `GET http://localhost:5000/api/health`.
- La primera versión ofrece usuarios ficticios en `GET /api/auth/users`. Iniciar sesión con `POST /api/auth/login` y `{ "userId": "EST-001" }` (o cualquiera de los IDs listados). Enviar el token resultante como `Authorization: Bearer <token>`.
- Chat: `POST /api/chat/messages`, cuerpo `{ "message": "¿Cuál es la nota mínima para aprobar?" }`.

## Compilación y pruebas

Desde la raíz del repositorio:

```powershell
dotnet restore backend/TutorFlow.sln
dotnet build backend/TutorFlow.sln --no-restore
dotnet test backend/TutorFlow.sln --no-build
```

Las pruebas locales usan un adaptador falso y no llaman a Gemini ni consumen cuota. Cubren respuestas válidas, el filtrado anti-inyección, el alcance de datos por rol, el error controlado de cuota y la falta de `GEMINI_API_KEY`. Las pruebas reales (pregunta simple y flujo E2E) se omiten por defecto. El runner E2E carga dinámicamente cada caso de `initial_docs/preguntas_ejemplo.json`, inicia `initial_docs/mock-api/server.js` con Node.js en un puerto local disponible, comprueba `/salud` y consulta la API mock por HTTP. El backend obtiene el catálogo en vivo mediante `/programas` y `/programas/{id}/estudiantes`, y construye el contexto con `/estudiantes/{id}`, `/notas` y `/asistencia`; el coordinador se resuelve con `/coordinadores/{id}` cuando corresponde. El runner apaga el proceso iniciado al terminar. No lee `datos.json` directamente desde el proyecto de pruebas: ese archivo solo lo carga el servidor mock como fuente de su propia API.

Para ejecutar las llamadas reales después de configurar `.env` o el entorno, ejecuta:

```powershell
$env:RUN_GEMINI_INTEGRATION_TESTS = "true"
dotnet test backend/TutorFlow.sln --filter FullyQualifiedName~GeminiIntegrationTests
dotnet test backend/TutorFlow.sln --filter FullyQualifiedName~InitialQuestionsE2ETests --logger "console;verbosity=detailed"
```

La suite opt-in envía una pregunta simple y procesa los diez casos del JSON; el caso de acceso no autorizado debe detenerse antes de llamar a Gemini. Estas solicitudes pueden consumir cuota. Si el `.env` local todavía selecciona `gemini-2.5-flash` y la clave no tiene acceso a ese modelo, establece `GEMINI_MODEL=gemini-3.5-flash-lite` en el entorno para ejecutar las pruebas con el modelo predeterminado vigente.

## Rutas

| Método y ruta | Auth | Uso |
|---|---|---|
| `GET /api/health` | No | Estado y proveedor Gemini. |
| `GET /api/auth/users` | No | Usuarios ficticios disponibles para pruebas. |
| `POST /api/auth/login` | No | Crea una sesión para el ID seleccionado. |
| `POST /api/auth/logout` | Bearer | Invalida la sesión actual. |
| `POST /api/chat/messages` | Bearer | Valida entrada, permisos, arma el contexto y llama al adaptador. |

El estudiante solo recibe contexto de su propio expediente; cada coordinador recibe contexto de sus programas asignados. Las referencias explícitas a IDs de estudiantes fuera de ese conjunto se rechazan con 403.

## Decisiones de simplicidad

- **Un token opaco por sesión:** evita añadir JWT/refresh u otro token en esta prueba; se genera con bytes aleatorios criptográficos y se mantiene en memoria del proceso. Al reiniciar el backend, las sesiones terminan.
- **Sin refresh tokens ni doble token:** mantiene pequeño el flujo de login y chat centrado en demostrar roles y permisos.
- **Sin endurecimiento avanzado de seguridad/CSRF/CORS:** el cliente envía el bearer token en un encabezado, no en una cookie; solo se habilita el origen local de Vite. Es un prototipo local, no una configuración para producción.
- **Filtro anti-inyección heurístico:** bloquea frases obvias, como intento de ignorar instrucciones o revelar el prompt. Es una barrera de demostración, no un detector exhaustivo.
- **Agente cloud desacoplado:** `IAgentClient` define el límite y `GeminiAgentClient` llama al SDK oficial. Una sustitución futura del proveedor no cambia el pipeline.
- **API de notas separada:** el backend consulta la mock API si está disponible. Los datos del estudiante no se incluyen cuando la mock API no responde; el conocimiento normativo continúa disponible.
