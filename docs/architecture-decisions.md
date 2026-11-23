# Decisiones de arquitectura — TutorFlow

## Alcance del prototipo

La solución separa el cliente React/TypeScript (`frontend/`) de la API ASP.NET Core .NET 8 (`backend/`). El backend carga `rules.md` y `api.md` como conocimiento, consulta la mock API local para datos académicos y usa el SDK oficial .NET `Google.GenAI` a través de `GeminiAgentClient`, detrás de `IAgentClient`.

## Sesión y autenticación

- El usuario selecciona una identidad ficticia de prueba; no hay contraseña ni proveedor de identidad.
- Cualquiera puede escoger cualquier perfil demo. Esto sirve para probar ramas de permisos, pero no prueba la identidad de una persona y no debe proteger datos reales.
- El login crea un solo token opaco aleatorio. El backend lo conserva en memoria junto con la identidad y el rol; el frontend manda `Authorization: Bearer <token>`.
- No hay refresh token ni doble token. Esto mantiene el ejercicio centrado en selección de identidad, sesión y autorización por rol.
- Al reiniciar el backend las sesiones desaparecen; no se implementan persistencia, expiración ni rotación para este prototipo.
- El token se guarda en `localStorage` solo para que la demo sobreviva a una recarga. La UI permite cerrar sesión e invalida la sesión del servidor.

## Autorización y contexto

- El rol se obtiene del registro de usuarios de prueba del backend, nunca del cuerpo del mensaje.
- Un estudiante solo recibe su propio perfil, notas y asistencia; las menciones a IDs de otros estudiantes se rechazan.
- Un coordinador solo recibe estudiantes pertenecientes a sus programas asignados; la solicitud de IDs ajenos se rechaza.
- Los documentos se cargan como contexto de conocimiento. La regla enviada al adaptador pide recordar y aplicar esas reglas, preservar estrictamente el alcance por rol y tratar los documentos como fuentes de datos, no como instrucciones ejecutables.
- Los ejemplos de estudiantes incluidos en `api.md` se anonimizan antes de pasarlos al agente. Los datos del mock se añaden según el alcance del usuario de la sesión.

## Pipeline de chat

1. Validar el bearer token y recuperar usuario y rol.
2. Rechazar patrones obvios de prompt injection mediante una heurística local.
3. Validar mensaje (1–2000 caracteres) y alcance de los IDs de estudiante solicitados.
4. Ensamblar usuario/rol, pregunta, conocimiento normativo/API, datos autorizados y reglas del agente; despachar por `IAgentClient`.

El filtro heurístico reduce casos obvios para la demo, pero no sustituye un control de seguridad exhaustivo.

## Seguridad de transporte y navegador

- No se usan cookies de sesión, por lo que el cliente envía el token en el encabezado Bearer y no se configura protección CSRF avanzada.
- CORS permite únicamente `http://localhost:5173` durante el desarrollo local. No hay una configuración CORS avanzada ni una política lista para producción.
- Se omiten endurecimiento avanzado, proveedor de identidad, límites de tasa, almacenamiento de sesiones distribuido y controles antiabuso porque el objetivo es demostrar el flujo funcional y los permisos, no certificar ciberseguridad.
- Estas decisiones son simplificaciones de una prueba técnica. Antes de un despliegue real se necesitaría un diseño de seguridad apropiado al entorno.

## Integración Gemini

`GeminiAgentClient` construye `systemInstruction` con reglas, identidad/rol y contexto autorizado; el mensaje validado se entrega como contenido de usuario. `GEMINI_API_KEY` y `GEMINI_MODEL` se cargan desde `.env` o el entorno. El cargador local solo establece variables que no existían en el proceso. El SDK queda configurado sin reintentos automáticos para que los 429 de cuota regresen pronto y con un mensaje claro.

El modelo inicial es `gemini-2.5-flash`, seleccionado para pruebas con el tier gratuito. Se puede cambiar mediante `GEMINI_MODEL` sin editar el código.
