# TutorFlow Angular

Frontend Angular 21 + TypeScript. `BackendApiService` contiene las llamadas HTTP al backend; el componente `App` solo coordina la vista del selector de usuarios y el chat.

## Desarrollo

```powershell
npm install
npm start
```

La aplicación se sirve en `http://localhost:5173`. La URL del backend local se configura en `public/config.json` (`apiBaseUrl`). En Docker, el servidor entrega `/config.json` a partir de `BACKEND_API_URL`, por lo que puede cambiarse en `.env` sin recompilar Angular.

## Build y pruebas

```powershell
npm run build
npm test
```

Las pruebas Vitest cubren la capa HTTP con `HttpTestingController` y la selección/envío de mensajes del componente. El flujo completo de sistema se valida con el runner `docker-e2e` desde la raíz.
