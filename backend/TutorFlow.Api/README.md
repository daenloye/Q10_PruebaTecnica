# TutorFlow.Api

Proyecto ASP.NET Core minimal API (`net8.0`). Ver [README del backend](../README.md) para ejecución, endpoints, permisos y decisiones de arquitectura.

## Configuración

- `MockApi:BaseUrl` en `appsettings.json` o variable `MockApi__BaseUrl`; valor local: `http://localhost:4000/`.
- El registro DI usa `DemoAgentClient`; la integración cloud queda pendiente detrás de `IAgentClient`.
- `rules.md` y `api.md` se copian al directorio `Knowledge` de salida al compilar.
