# Decisiones de arquitectura — TutorFlow

## Alcance

TutorFlow separa la interfaz Angular/TypeScript (`frontend/`) de la API ASP.NET Core .NET 8 (`backend/`). `BackendApiService` concentra las llamadas REST y el componente solo gestiona el selector/chat. La URL REST se carga desde configuración runtime (`BACKEND_API_URL` en Docker o `frontend/public/config.json` en desarrollo local). El backend carga `rules.md` y `api.md`, consulta la mock API para datos académicos y envía el contexto autorizado a Gemini mediante `IAgentClient`.

## Identidad de prueba y API directa

- La interfaz presenta en una sola vista el selector **Preguntar como** y el chat. Cada envío contiene el ID elegido y la pregunta.
- `GET /api/users` publica los perfiles disponibles y sus roles para el selector y los consumidores REST.
- `POST /api/chat/messages` recibe `{ "userId": "EST-001", "message": "¿Cuál es mi promedio?" }`. No requiere una petición de login previa, token de sesión ni cookie.
- La identidad y rol se resuelven en el backend desde el directorio controlado de usuarios y los registros de la mock API. El cliente no puede fijar el rol ni los programas autorizados en el cuerpo.
- Esta simplificación agiliza pruebas funcionales: se puede elegir un rol en un clic o probar el servicio desde curl, Postman y scripts sin abrir el frontend.
- Los perfiles son ficticios y `userId` se transmite en texto plano. Cualquiera que pueda alcanzar esta API de demostración puede elegir otro perfil; esto **no autentica** a una persona ni protege información real.

## Pipeline de consulta y permisos

1. Rechazar `userId` vacío, desconocido o asociado a un rol no admitido (`Student`/`Coordinator`).
2. Rechazar preguntas vacías, mayores de 2000 caracteres o con patrones obvios de prompt injection.
3. Resolver en servidor el perfil y los programas autorizados. Un estudiante solo obtiene su expediente; un coordinador solo obtiene estudiantes de sus programas. Las referencias fuera del alcance se rechazan con HTTP 403 antes de llamar a Gemini.
4. Construir contexto de `rules.md`, `api.md` y datos académicos permitidos. Enviar a Gemini la pregunta, identidad/rol y la instrucción explícita de respetar reglas y alcance.

La heurística anti-inyección bloquea ejemplos obvios y no garantiza detectar todas las técnicas. Las reglas de acceso importantes también se aplican en el backend antes del agente; no se confía en la respuesta del modelo como control de autorización.

## Qué cambiaría en producción

No se aceptaría un `userId` arbitrario enviado por el cliente como prueba de identidad. Se integraría autenticación OAuth 2.0 / OpenID Connect (por ejemplo, Authorization Code con PKCE), tokens JWT firmados con expiración y refresh tokens administrados con rotación/revocación según el proveedor, y validación de firma, emisor, audiencia y expiración en servidor. El usuario y sus roles/permisos saldrían de claims criptográficos verificados; el cliente no podría seleccionar sus propios privilegios.

También se configurarían rate limits por usuario e IP, CORS estricto por origen, HTTPS/TLS extremo a extremo, auditoría de accesos y decisiones, gestión centralizada de secretos, monitoreo y controles de abuso. La selección libre de perfiles, usuarios ficticios y los controles locales actuales son exclusivos de la prueba técnica, no una configuración lista para producción.

## Integración Gemini

`GeminiAgentClient` combina la instrucción de reglas, el contexto autorizado y el perfil ya resuelto en servidor. `GEMINI_API_KEY` y `GEMINI_MODEL` vienen del entorno o `.env`; el modelo se puede cambiar sin modificar el código. Las respuestas de cuota, configuración, red y servicio se traducen a errores controlados.

### Estilo de respuesta

Gemini responde en español, con el dato o conclusión al inicio y normalmente en 1–3 frases o viñetas breves. Omite saludos, repeticiones y cierres; añade desgloses solo cuando se solicitan o aclaran el resultado. La configuración usa temperatura `0.1` y un máximo de `768` tokens de salida para favorecer respuestas objetivas sin cortar listados razonables. Esta preferencia no reduce la obligación de responder todos los puntos solicitados, usar valores calculados por TutorFlow, seguir `rules.md`/`api.md`, no inventar información ni exceder el contexto permitido por el rol. La validación de permisos se realiza en el backend antes de invocar a Gemini.
