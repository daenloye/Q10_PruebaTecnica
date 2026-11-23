# TutorFlow frontend

Interfaz React + TypeScript + Vite. Incluye selector de usuarios demo, login de token único y vista de chat.

## Requisitos y ejecución

- Node.js 18 o superior y npm.
- `npm install`
- Backend .NET activo en `http://localhost:5000` y mock API en `http://localhost:4000` para consultar datos académicos personalizados.
- `npm run dev` abre la interfaz en `http://localhost:5173`.
- Copia `.env.example` a `.env` y configura `VITE_API_BASE_URL` si el backend usa otro host/puerto.

El token único se conserva en `localStorage` para mantener la sesión al recargar la página. Cerrar sesión invalida el token en el backend.
