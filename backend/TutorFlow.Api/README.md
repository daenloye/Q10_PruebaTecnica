# TutorFlow.Api

Servicio ASP.NET Core minimal API (`net8.0`) para consultas de TutorFlow. Las instrucciones de operación completa están en [README de backend](../README.md); el contrato de rutas está en [docs/backend-endpoints.md](../../docs/backend-endpoints.md).

## Configuración

- `GEMINI_API_KEY`: clave de Gemini, requerida para generar respuestas.
- `GEMINI_MODEL`: modelo; valor predeterminado `gemini-3.5-flash-lite`.
- `MockApi__BaseUrl`: URL de la API mock, predeterminada `http://localhost:4000/` en ejecución local.
- `Frontend__Origin`: origen permitido para el navegador, predeterminado `http://localhost:5173`.
- En Docker Compose, los valores se cargan desde `.env` y el backend usa `http://mock-api:4000/` para comunicarse dentro de la red.

Desde la raíz, crea `.env` copiando `.env.example` y agrega la clave. `.env` y `.env.*` están ignorados por Git; solo las plantillas `.env.example` se deben versionar.

## Ejecutar

Con la API mock activa en el puerto 4000 y la configuración de Gemini disponible, ejecuta desde este directorio:

```powershell
dotnet run --urls http://localhost:5000
```

La API informa salud en `/api/health`. La API mock y el backend pueden ejecutarse juntos con `docker compose up -d --build` desde la raíz.

## Integración y comportamiento

El SDK oficial `Google.GenAI` se usa detrás de `IAgentClient`. El contexto incluye reglamento, contrato de API, perfil y datos que el rol puede consultar. Estudiantes quedan limitados a sus expedientes y coordinadores a sus programas. La validación de alcance ocurre antes de llamar al agente. Gemini recibe instrucciones para contestar en español y de forma breve, sin alterar datos o permisos.

Los errores de configuración, cuota, servicio y timeout se transforman en estados HTTP controlados. Los logs pueden incluir detalles técnicos sanitizados del proveedor, pero no deben exponer la API key.

## Pruebas

Desde la raíz del repositorio:

```powershell
dotnet test backend/TutorFlow.sln
```

Las pruebas optativas con Gemini se habilitan mediante `RUN_GEMINI_INTEGRATION_TESTS=true`; requieren una clave válida y pueden consumir cuota.
