# TutorFlow

TutorFlow es un prototipo de asistente académico para responder consultas con el reglamento institucional, la especificación de la API simulada y los datos autorizados por rol.

## Estructura

- `backend/`: API ASP.NET Core con .NET 8, sesión única, autorización por rol y adaptador desacoplado del agente.
- `frontend/`: interfaz React + TypeScript para seleccionar usuario demo e interactuar por chat.
- `docs/architecture-decisions.md`: decisiones de simplicidad y alcance de seguridad del prototipo.
- `rules.md` y `api.md`: fuentes Markdown de conocimiento que el backend carga.
- `initial_docs/mock-api/`: API de datos simulados existente.

## Inicio con Docker

Requisito: Docker Desktop o Docker Engine con Docker Compose v2 activo.

1. Copia `.env.example` a `.env` si aún no existe y conserva/configura `GEMINI_API_KEY` y `GEMINI_MODEL`.
2. Edita en `.env` `MOCK_API_URL` (URL que usa el backend dentro de la red Compose) y `MOCK_API_PORT` (puerto de la mock API publicado en el host). Si cambias el puerto interno del servicio, actualiza también el puerto en `MOCK_API_URL` y en `docker-compose.yml`. `BACKEND_PORT` y `FRONTEND_PORT` cambian los puertos publicados de esos servicios.
3. Desde la raíz, ejecuta `docker compose up -d --build`.
4. Revisa el arranque con `docker compose ps` y `docker compose logs -f`.
5. Abre el frontend y selecciona un usuario de prueba.

URLs predeterminadas:

- Frontend/chat: <http://localhost:5173>
- Backend: <http://localhost:5000>; salud: <http://localhost:5000/api/health>
- Mock API: <http://localhost:4000>; salud: <http://localhost:4000/salud>

Para detener los servicios usa `docker compose down`. `.env` se usa solo localmente, está excluido de Git y del contexto de build; no compartas su clave de Gemini.

## Inicio manual de desarrollo

1. Copia `.env.example` a `.env` en la raíz y define `GEMINI_API_KEY` (y opcionalmente `GEMINI_MODEL`). Nunca agregues `.env` a Git.
2. Inicia la API académica para habilitar datos personalizados: desde `initial_docs/mock-api/`, ejecuta `node server.js`.
3. Inicia el backend .NET 8: `cd backend/TutorFlow.Api` y `dotnet run --urls http://localhost:5000`.
4. Inicia el frontend: `cd frontend`, `npm install`, `npm run dev`.
5. Abre `http://localhost:5173` y selecciona cualquiera de los usuarios ficticios. El chat envía el token de sesión al backend.

El backend y el frontend incluyen sus propios README con detalles. El backend usa el SDK oficial de Google Gemini; consulta `docs/architecture-decisions.md` para el flujo de contexto y permisos.
