# Arquitectura y decisiones — TutorFlow

## Objetivo y alcance

TutorFlow es un prototipo funcional de asistente académico. Su arquitectura separa la interfaz Angular/TypeScript, la API ASP.NET Core sobre .NET 8 y la API académica mock de Node. Docker Compose ejecuta los tres servicios. El backend consulta los datos académicos por HTTP, incorpora `rules.md` y `api.md`, y llama a Gemini mediante `IAgentClient`.

El alcance es validar integración y reglas con datos ficticios. No se presenta como sistema listo para operar con expedientes reales.

## Stack del backend: .NET

Se conserva .NET 8 o superior, conforme al stack solicitado para la prueba. ASP.NET Core permite mantener validación, autorización por alcance, composición de contexto e integración Gemini en una API REST independiente de la interfaz. Docker usa SDK `10.0.404` para compilar y una imagen runtime compatible.

## Interfaz: Angular en lugar de React

El frontend entregado está implementado en Angular y TypeScript. Angular aporta una organización modular y convenciones consistentes para separar componentes y servicios. `BackendApiService` concentra las llamadas HTTP, mientras la vista se ocupa del selector de perfil y del chat. Esta separación favorece mantenimiento y ampliación del cliente.

Durante las iteraciones del prototipo en React, los problemas repetidos con el ciclo de vida de efectos y la confusión entre código servido y builds anteriores hicieron más difícil estabilizar la validación en navegador. Se migró a Angular para entregar una estructura más clara y un proceso reproducible. La configuración actual de Docker compila y sirve Angular; React fue retirado.

## Flujo de una consulta

1. El usuario elige un perfil ficticio y escribe una pregunta en la interfaz, o invoca directamente la API REST.
2. El cliente envía `userId` y `message` a `POST /api/chat/messages`.
3. El backend verifica el perfil, valida el contenido y bloquea patrones evidentes de prompt injection.
4. El backend resuelve rol y programas desde sus fuentes controladas, consulta por HTTP la API académica mock y limita los datos antes de formar el contexto con `rules.md` y `api.md`.
5. La instrucción del agente trata pregunta y documentos como datos, aplica las reglas, el contexto y el alcance autorizados, y solicita una respuesta breve en español.
6. Gemini devuelve la respuesta al backend, que la entrega al cliente REST.

La defensa anti-inyección por patrones es una capa heurística, no una garantía frente a todas las técnicas. La autorización por alcance se ejecuta en el backend antes de llamar al modelo; no depende de que Gemini aplique por sí mismo el control.

## Identidad y permisos en la prueba

- `GET /api/users` publica los perfiles disponibles con su rol y programa.
- `POST /api/chat/messages` recibe `{ "userId": "EST-001", "message": "¿Cuál es mi promedio?" }`.
- No hay login, token, cookie ni encabezado de autorización. Esto reduce pasos para probar roles desde la interfaz, Postman, `curl` y scripts.
- El ID es ficticio y se transmite en texto plano. El backend busca su rol y alcance; no confía en un rol o lista de programas enviados por el cliente.
- Un estudiante solo consulta sus propios datos. Un coordinador solo consulta estudiantes de sus programas asignados. Las solicitudes fuera de alcance se rechazan antes de invocar Gemini.
- La selección libre de perfiles no prueba identidad. La API y los datos son solo de demostración y no deben usarse para información personal real.

## Seguridad: prototipo y producción

En esta fase se priorizó demostrar funcionamiento e integración. Se omitieron login y controles avanzados propios de producción. Sí permanecen validación de datos, rechazo de patrones obvios de inyección y autorización de los registros consultados dentro del alcance de cada rol. Estos controles funcionales no sustituyen una autenticación robusta ni convierten la demo en un entorno seguro para datos reales.

Una versión productiva debería incluir, según el diseño de identidad, OAuth 2.0/OpenID Connect, credenciales o JWT firmados y expirables, refresh tokens con políticas de rotación/revocación, autorización desde claims verificados en servidor, protección CSRF cuando se usen cookies o sesiones de navegador, HTTPS/TLS, CORS estricto, rate limiting por usuario/IP, auditoría y gestión centralizada de secretos. El cliente nunca debería elegir sus permisos mediante un ID arbitrario.

## Datos, configuración y despliegue

- El backend consulta la API mock por HTTP para catálogo, perfiles, notas y asistencia; no usa el JSON de ejemplo como fuente directa durante el flujo de consulta.
- `MOCK_API_URL` define el destino de esa API desde el backend. Para comunicación entre contenedores se usa por defecto `http://mock-api:4000/`.
- `BACKEND_API_URL` se publica a Angular en tiempo de ejecución. La dirección del backend visible desde el navegador es por defecto `http://localhost:5000`.
- `GEMINI_API_KEY` y `GEMINI_MODEL` se cargan desde el entorno/`.env`. El `.env` real está ignorado por Git; `.env.example` documenta nombres y valores de muestra sin secreto.
- Docker Compose construye y orquesta `mock-api`, `backend` y `frontend`, con dependencias de salud para reducir fallos de orden de arranque.

## Integración y estilo de Gemini

`GeminiAgentClient` ensambla reglas, perfil resuelto, permisos y contexto autorizado. La configuración actual usa temperatura `0.1` y hasta `768` tokens de salida. La respuesta solicitada debe poner primero el dato o conclusión, evitar introducciones y cierres y normalmente ocupar 1–3 frases o viñetas breves. Debe incluir todos los datos que la pregunta solicita, utilizar valores calculados por TutorFlow, citar secciones cuando aplique y declarar brevemente cuando la información no esté disponible. La concisión no modifica exactitud, permisos ni las instrucciones de seguridad.

Los errores de configuración, cuota, red, disponibilidad del modelo y timeout se traducen a respuestas controladas; los logs técnicos no deben revelar la API key.

## Pruebas y documentos relacionados

Las pruebas unitarias de Angular cubren servicio HTTP y componente. Las pruebas .NET cubren pipeline, validaciones y permisos; las pruebas optativas Gemini pueden consumir cuota. El runner `docker-e2e` verifica los servicios y consulta los endpoints en ejecución, incluida una llamada real a Gemini.

Consulta [README.md](../README.md) para arranque, variables y URLs; [contrato REST](backend-endpoints.md) para detalles de integración; y [presentación de entrega](entrega.md) para el resumen de cierre.
