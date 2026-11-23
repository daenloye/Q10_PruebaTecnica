# TutorFlow backend

API ASP.NET Core sobre .NET 8. El backend autentica usuarios ficticios, crea una sesión opaca única en memoria, aplica validaciones y permisos por rol, carga `rules.md` y `api.md`, y despacha un payload estructurado mediante `IAgentClient`.

## Requisitos y ejecución

- .NET SDK 8 o superior.
- El selector de usuario no pide contraseña: identifica perfiles ficticios para recorrer los permisos en la demo y no constituye autenticación real.
- API mock requerida para incluir datos personalizados: Node.js 18+, desde `initial_docs/mock-api/`, `node server.js` (puerto 4000). El chat conserva las consultas normativas si el mock no está disponible.
- Desde `backend/TutorFlow.Api/`: `dotnet run --urls http://localhost:5000`.
- Salud: `GET http://localhost:5000/api/health`.
- La primera versión ofrece usuarios ficticios en `GET /api/auth/users`. Iniciar sesión con `POST /api/auth/login` y `{ "userId": "EST-001" }` (o cualquiera de los IDs listados). Enviar el token resultante como `Authorization: Bearer <token>`.
- Chat: `POST /api/chat/messages`, cuerpo `{ "message": "¿Cuál es la nota mínima para aprobar?" }`.

## Rutas

| Método y ruta | Auth | Uso |
|---|---|---|
| `GET /api/health` | No | Estado y proveedor demo. |
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
- **Agente cloud desacoplado:** `IAgentClient` define el límite. `DemoAgentClient` devuelve fragmentos de contexto con coincidencia de palabras y declara que la integración cloud está pendiente. Sustituirlo por un adaptador del proveedor no cambia el pipeline.
- **API de notas separada:** el backend consulta la mock API si está disponible. Los datos del estudiante no se incluyen cuando la mock API no responde; el conocimiento normativo continúa disponible.
