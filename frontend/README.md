# TutorFlow frontend

Interfaz React + TypeScript + Vite de una sola vista con selector **Preguntar como** y chat. El selector se alimenta desde `GET /api/users`; cada consulta envía `userId` y `message` a `POST /api/chat/messages`, sin flujo de login ni almacenamiento de tokens.

Configura `VITE_API_BASE_URL` con la URL del backend. En Compose la URL predeterminada del navegador es `http://localhost:5000`.
