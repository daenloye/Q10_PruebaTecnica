# TutorFlow

TutorFlow es un prototipo de asistente académico para responder consultas con el reglamento institucional, la especificación de la API simulada y los datos autorizados por rol.

## Estructura

- `backend/`: API ASP.NET Core con .NET 8, sesión única, autorización por rol y adaptador desacoplado del agente.
- `frontend/`: interfaz React + TypeScript para seleccionar usuario demo e interactuar por chat.
- `docs/architecture-decisions.md`: decisiones de simplicidad y alcance de seguridad del prototipo.
- `rules.md` y `api.md`: fuentes Markdown de conocimiento que el backend carga.
- `initial_docs/mock-api/`: API de datos simulados existente.

## Inicio local

1. Copia `.env.example` a `.env` en la raíz y define `GEMINI_API_KEY` (y opcionalmente `GEMINI_MODEL`). Nunca agregues `.env` a Git.
2. Inicia la API académica para habilitar datos personalizados: desde `initial_docs/mock-api/`, ejecuta `node server.js`.
3. Inicia el backend .NET 8: `cd backend/TutorFlow.Api` y `dotnet run --urls http://localhost:5000`.
4. Inicia el frontend: `cd frontend`, `npm install`, `npm run dev`.
5. Abre `http://localhost:5173` y selecciona cualquiera de los usuarios ficticios. El chat envía el token de sesión al backend.

El backend y el frontend incluyen sus propios README con detalles. El backend usa el SDK oficial de Google Gemini; consulta `docs/architecture-decisions.md` para el flujo de contexto y permisos.
