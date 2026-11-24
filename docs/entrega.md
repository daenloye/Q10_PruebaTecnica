# Presentación de entrega — TutorFlow

## Presentación

TutorFlow es una prueba técnica funcional de un asistente académico. Integra una interfaz Angular, una API REST en .NET 8, una API mock académica de Node y el servicio Gemini. Los documentos normativos y los datos ficticios permiten probar consultas académicas con respuestas breves y con límites por tipo de usuario.

## Qué se entrega

- Una vista web para seleccionar un perfil de estudiante o coordinador, escribir una pregunta y leer la respuesta.
- Una API REST que también se puede consumir sin abrir la interfaz.
- Validación de identidad de demostración, rol y alcance de datos ejecutada en servidor.
- Consulta HTTP a la API académica mock y contexto de negocio basado en `rules.md` y `api.md`.
- Integración con Gemini, pruebas unitarias y runner E2E mediante Docker Compose.
- Documentación de arquitectura, decisiones y contrato REST.

## Cómo funciona

El cliente envía un perfil de demostración y la pregunta. El backend valida el mensaje, rechaza patrones evidentes de inyección, determina los permisos desde sus propios datos y consulta únicamente los registros que puede compartir para ese perfil. Luego prepara el contexto normativo y académico y lo envía a Gemini. La respuesta se devuelve al cliente de forma concisa. Los estudiantes quedan limitados a su propio expediente; los coordinadores, a sus programas asignados.

## Decisiones finales

- **.NET:** se mantiene el backend solicitado en ASP.NET Core sobre .NET 8 o superior.
- **Migración de React a Angular:** React fue la opción inicial porque era el stack de interfaz indicado al comienzo de la prueba. Durante su estabilización se repitieron problemas con efectos del componente y discrepancias entre el código y los builds servidos. La decisión posterior de migrar a Angular permitió usar una estructura modular, un servicio HTTP separado de la vista y una base más clara para mantenimiento y crecimiento. La interfaz de esta entrega es Angular.
- **Sin login en la demo:** enviar un ID ficticio agiliza las pruebas de roles desde el navegador o REST. El ID no autentica a la persona ni debe emplearse con datos reales.
- **Seguridad:** la entrega conserva validación de mensajes, filtro heurístico de inyección y autorización de los datos por alcance. Omite autenticación y controles avanzados de producción. Para producción se contemplan OAuth/OIDC, tokens firmados con expiración, refresh tokens, claims verificados, CSRF cuando corresponda, HTTPS/TLS, CORS estricto, limitación de tasa, auditoría y gestión segura de secretos.
- **API académica:** el backend consulta la API mock en ejecución por HTTP. `MOCK_API_URL` permite cambiar el destino sin alterar código.
- **Gemini y secretos:** `GEMINI_API_KEY` y `GEMINI_MODEL` se definen en `.env`; el archivo local no se versiona. `.env.example` es la plantilla segura.
- **Contenedores y respuestas:** Docker Compose levanta API mock, backend y frontend. Gemini tiene una preferencia de respuesta directa y breve, con límites que no sustituyen las reglas ni el permiso de acceso.

## Configurar y levantar

1. Desde la raíz, copia la plantilla: `Copy-Item .env.example .env`.
2. Edita `.env` y agrega una clave válida en `GEMINI_API_KEY`. Conserva o ajusta `GEMINI_MODEL`, puertos y URLs según el entorno. No compartas `.env` ni lo agregues a Git.
3. Construye y arranca: `docker compose up -d --build`.
4. Comprueba los servicios: `docker compose ps`.

Para detenerlos: `docker compose down`. La guía principal contiene instrucciones adicionales y los comandos de pruebas.

## Puntos de consulta

- Interfaz: [http://localhost:5173/](http://localhost:5173/)
- Estado backend: [http://localhost:5000/api/health](http://localhost:5000/api/health)
- Usuarios y roles: [http://localhost:5000/api/users](http://localhost:5000/api/users)
- Estado API mock: [http://localhost:4000/salud](http://localhost:4000/salud)
- Programas mock: [http://localhost:4000/programas](http://localhost:4000/programas)

## Documentos de referencia

- [Guía principal y validación](../README.md)
- [Arquitectura y decisiones](architecture-decisions.md)
- [Contrato de endpoints](backend-endpoints.md)
- [Reglas académicas](../rules.md)
- [Contrato de API académica](../api.md)
