# TutorFlow.Api

Proyecto ASP.NET Core minimal API (`net8.0`). Ver [README del backend](../README.md) para ejecución, endpoints, permisos y decisiones de arquitectura.

## Configuración

- `MockApi:BaseUrl` en `appsettings.json` o variable `MockApi__BaseUrl`; valor local: `http://localhost:4000/`.
- `GEMINI_API_KEY` y `GEMINI_MODEL` se cargan desde el entorno; el modelo predeterminado es `gemini-3.5-flash-lite`.
- Desde la raíz, copia `.env.example` a `.env`, coloca allí la clave de Gemini API y reinicia el backend. El cargador busca el `.env` del repositorio; `.env` y `.env.*` están ignorados por Git, salvo las plantillas `.env.example`.
- Se usa el SDK oficial `Google.GenAI` mediante `GeminiAgentClient`, registrado detrás de `IAgentClient`. Las instrucciones de sistema llevan reglas, contexto autorizado e identidad/rol; el prompt validado se envía como contenido de usuario.
- Errores controlados: 503 si falta/se rechaza la clave, el modelo no está disponible o Gemini presenta alta demanda; 429 por cuota; 502 por otros errores de servicio/red; 504 por timeout. Los estados transitorios usan hasta 3 intentos con backoff exponencial y el log registra estado/modelo/detalle sanitizado del proveedor.
- `rules.md` y `api.md` se copian al directorio `Knowledge` de salida al compilar.
