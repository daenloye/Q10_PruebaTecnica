# TutorFlow — Frontend Angular

Interfaz Angular 21 y TypeScript. Presenta el selector **Preguntar como**, el chat y las respuestas. `BackendApiService` contiene la comunicación HTTP con el backend para que el componente principal se concentre en la interfaz.

## Requisitos y configuración

- Node.js compatible con Angular 21 y npm.
- La URL REST local se configura en `public/config.json` (`apiBaseUrl`). Al correr en Docker, el servidor publica `/config.json` con el valor de `BACKEND_API_URL`, editable en el `.env` de la raíz y aplicado al arrancar el contenedor.
- La URL que Angular usa debe ser accesible desde el navegador. En la configuración local predeterminada es `http://localhost:5000`.

## Desarrollo local

Desde `frontend/`:

```powershell
npm ci
npm start
```

La aplicación queda en `http://localhost:5173`. Para completar el flujo, el backend debe estar activo en la URL configurada y la API mock debe ser accesible desde el backend.

## Compilación y pruebas

```powershell
npm run build
npm test
```

Las pruebas usan `HttpTestingController` para verificar las solicitudes del servicio y validan la selección del usuario y envío de mensajes desde el componente. El runner E2E del repositorio prueba el frontend Angular junto con backend y API mock en Docker.

## Ejecución Docker

Desde la raíz del repositorio configura `.env` según el [README principal](../README.md#configuración-y-arranque-con-docker) y ejecuta:

```powershell
docker compose up -d --build frontend
docker compose ps
```

Consulta [arquitectura y decisiones](../docs/architecture-decisions.md), [contrato REST](../docs/backend-endpoints.md) y [presentación de entrega](../docs/entrega.md).
