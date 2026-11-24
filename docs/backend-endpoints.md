# Contrato REST del backend TutorFlow

Base URL local: `http://localhost:5000` (puerto configurable con `BACKEND_PORT`). El backend ASP.NET Core escucha HTTP; todas las rutas usan el prefijo `/api`. Las respuestas JSON usan nombres camelCase y `Content-Type: application/json`. No se requiere sesión ni encabezado de autorización para este prototipo. El identificador recibido se resuelve en el directorio del servidor; los permisos no se toman del cliente. El navegador está sujeto a CORS: por defecto se permite `http://localhost:5173`; configura `FRONTEND_ORIGIN` para el origen único de otro frontend. Clientes no navegador como `curl` no aplican CORS.

## `GET /api/health`

Comprueba que el backend está activo.

**Respuesta `200 OK`:**

```json
{
  "status": "ok",
  "agentProvider": "gemini"
}
```

## `GET /api/users`

Lista los usuarios demo configurados y los usuarios que el backend puede descubrir en la API académica mock. No acepta parámetros.

**Respuesta `200 OK`:** arreglo de `DemoUser` (puede estar vacío si el directorio no contiene usuarios):

| Campo | Tipo | Nulo | Descripción |
|---|---|---:|---|
| `id` | string | no | Identificador para enviar en `userId`. |
| `name` | string | no | Nombre mostrado. |
| `role` | string | no | `Student` o `Coordinator`. |
| `programId` | string \| null | sí | Programa de un estudiante; `null` para coordinadores. |
| `programIds` | string[] | no | Programas autorizados para un coordinador; vacío para estudiantes. |

Ejemplo:

```json
[
  { "id": "EST-001", "name": "Laura Gómez", "role": "Student", "programId": "ING-SIS", "programIds": [] },
  { "id": "COORD-01", "name": "Marcela Ríos", "role": "Coordinator", "programId": null, "programIds": ["ING-SIS"] }
]
```

El conjunto demo incluye estudiantes `EST-001`–`EST-006` y coordinadores `COORD-01`/`COORD-02`; la lista efectiva puede ampliarse dinámicamente desde la API mock.

## `POST /api/chat/messages`

Valida el usuario, filtra intentos evidentes de prompt injection, valida la pregunta, construye el contexto autorizado por rol y consulta el agente Gemini.

**Encabezado requerido:** `Content-Type: application/json`.

**Cuerpo de entrada (`ChatRequest`):**

| Campo | Tipo JSON | Obligatorio | Restricciones |
|---|---|---:|---|
| `userId` | string | sí | ID existente en el directorio; los permisos se resuelven en servidor. |
| `message` | string | sí | Entre 1 y 2000 caracteres después de recortar espacios. |

Ejemplo:

```json
{
  "userId": "EST-001",
  "message": "¿Cuál es la nota mínima para aprobar?"
}
```

**Respuesta `200 OK` (`ChatResponse`):**

| Campo | Tipo JSON | Descripción |
|---|---|---|
| `answer` | string | Respuesta del agente. |
| `sources` | string[] | Documentos/contextos citados. |
| `provider` | string | `gemini`. |

Ejemplo:

```json
{
  "answer": "La nota mínima para aprobar es 3.0 sobre 5.0.",
  "sources": ["rules.md", "api.md"],
  "provider": "gemini"
}
```

### Errores y permisos

| Estado | Código / condición | Forma de respuesta |
|---:|---|---|
| `400` | `invalid_user`, `unknown_user`, `invalid_role`, `prompt_rejected`, `invalid_message` | `{ "error": "código", "message": "descripción" }`. |
| `403` | Se menciona o intenta consultar información académica fuera del alcance del rol | Problem Details JSON con `title`, `status` (`403`) y `detail`; puede incluir `type`/`traceId`. |
| `429` | `gemini_quota_exceeded` | `{ "error": "gemini_quota_exceeded", "message": "..." }`. |
| `502` | Error de red, solicitud rechazada o servicio Gemini no disponible | `{ "error": "gemini_network_error\|gemini_request_rejected\|gemini_service_error", "message": "..." }`. |
| `503` | `gemini_not_configured`, `gemini_credentials_rejected`, `gemini_model_unavailable` o `gemini_overloaded` | `{ "error": "código", "message": "..." }`. Los errores transitorios se reintentan hasta dos veces con backoff exponencial. |
| `504` | `gemini_timeout` | `{ "error": "gemini_timeout", "message": "..." }`. |

Estudiantes (`Student`) pueden consultar sus propios datos; coordinadores (`Coordinator`) solo pueden consultar estudiantes de sus programas asignados. No se acepta un rol en el request. Una consulta que intente acceder a un ID/nombre no autorizado se rechaza antes de llamar al agente.

## Integración directa

```powershell
curl.exe http://localhost:5000/api/users
curl.exe -X POST http://localhost:5000/api/chat/messages `
  -H "Content-Type: application/json" `
  -d '{"userId":"EST-001","message":"¿Cuál es la nota mínima para aprobar?"}'
```

El contrato del servicio académico mock en `api.md` es independiente: el backend lo consume internamente y aplica sus propias validaciones de acceso.
