# TutorFlow

## Presentación

TutorFlow es un asistente académico desarrollado como prototipo funcional para la prueba técnica. Permite consultar el reglamento institucional y datos académicos de demostración con respuestas breves y contextualizadas. La solución integra una interfaz Angular, una API REST en ASP.NET Core y Gemini; el backend verifica el rol y el alcance de cada consulta antes de compartir información con el agente.

Los perfiles y registros incluidos son ficticios. La entrega demuestra el flujo y la integración entre componentes; no está destinada a proteger información académica real ni constituye una configuración productiva.

## Resultado de la entrega

- Backend ASP.NET Core sobre .NET 8, construido en Docker con SDK `10.0.404`.
- Interfaz Angular y TypeScript con selección de perfil y chat. `BackendApiService` centraliza las llamadas REST.
- API académica mock consultada por HTTP; los datos no se leen desde un JSON estático en el flujo del backend.
- Integración con Gemini configurada mediante variables de entorno.
- Validación de entradas, filtro de patrones obvios de prompt injection y límites de acceso por estudiante/coordinador aplicados en el backend.
- Pruebas unitarias de Angular y .NET, y un runner E2E Docker para los flujos integrados.

## Documentación

- [Arquitectura y decisiones](docs/architecture-decisions.md): stack, flujo, permisos y alcance de seguridad.
- [Presentación formal de entrega](docs/entrega.md): resumen ejecutivo, operación y puntos de consulta.
- [Contrato de endpoints del backend](docs/backend-endpoints.md): rutas, esquemas, respuestas, errores y ejemplos.
- [Reglas académicas](rules.md): síntesis normativa utilizada como conocimiento del asistente.
- [Contrato de la API académica](api.md): rutas y esquemas de la API mock.
- [Guía del backend](backend/README.md) y [guía de Angular](frontend/README.md).

## Funcionamiento

1. La persona elige un perfil de demostración en **Preguntar como** y escribe su consulta.
2. Angular envía `userId` y `message` a `POST /api/chat/messages`. También se puede invocar directamente desde cualquier cliente REST.
3. El backend resuelve el nombre, rol y programas permitidos desde sus fuentes de datos. No acepta el rol como dato confiable del cliente.
4. Valida el mensaje y bloquea patrones evidentes de inyección. Luego consulta por HTTP la API mock y compone el contexto con los documentos `rules.md`, `api.md` y los datos académicos autorizados.
5. Gemini recibe ese contexto, el perfil autorizado y la pregunta. Responde en español, de forma concisa y basada en la información disponible.

Estudiantes consultan únicamente su expediente; coordinadores, únicamente estudiantes de los programas asignados. Los intentos fuera de alcance se rechazan antes de llamar al agente. El filtrado de inyección es una defensa complementaria y no garantiza identificar todos los ataques.

## Decisiones finales

- **Backend .NET:** se conserva ASP.NET Core sobre .NET 8 o superior, según el stack solicitado y para integrar de forma directa las reglas, la API adjunta y Gemini.
- **Angular en lugar de React:** se adoptó una estructura modular con servicios dedicados, como `BackendApiService`, que separa la vista del acceso HTTP y facilita ampliar y mantener el cliente. Durante las iteraciones con React aparecieron problemas repetidos de ciclo de vida de efectos y de builds/servidor desactualizados que dificultaban validar la interfaz; la migración permitió estabilizar y simplificar esa estructura. La solución entregada usa Angular y no mantiene un servicio React.
- **Identidad de demostración:** no hay login ni token de sesión. Enviar un identificador ficticio permite probar distintos roles desde la UI o desde REST sin pasos previos. El servidor resuelve los permisos del perfil y no acepta roles declarados por el cliente.
- **Seguridad en esta fase:** se implementaron controles funcionales de validación, defensa básica ante inyección y autorización por alcance. Se omitieron autenticación real y controles avanzados de producción para facilitar la prueba integrada; esto no habilita el uso con datos reales.
- **Producción:** se requerirían autenticación OAuth 2.0/OpenID Connect, tokens firmados con expiración y renovación mediante refresh token, autorización basada en claims verificados, protección CSRF cuando corresponda al mecanismo de sesión/navegador, HTTPS/TLS, CORS estricto, limitación de tasa, auditoría y gestión centralizada de secretos.
- **Datos y configuración:** el backend consulta la API mock mediante HTTP. `MOCK_API_URL` permite cambiar su dirección sin modificar código; `GEMINI_API_KEY` y `GEMINI_MODEL` se configuran en `.env`, que Git ignora. Solo `.env.example`, sin credenciales reales, se versiona.
- **Despliegue y respuestas:** Docker Compose orquesta la API mock, el backend y Angular con dependencias y comprobaciones de salud. Gemini recibe una preferencia de respuesta concisa (temperatura `0.1`, máximo `768` tokens); esta preferencia no permite omitir datos pedidos ni rebajar reglas o permisos.

El detalle ampliado de estas decisiones está en [docs/architecture-decisions.md](docs/architecture-decisions.md).

## Requisitos

- Docker Desktop con Docker Compose.
- Una clave de Gemini API para probar el chat real. Las llamadas pueden estar sujetas a cuota del proveedor.
- Para desarrollo local: Node.js compatible con Angular 21 y .NET SDK 8 o superior. La configuración de contenedor de backend usa .NET SDK `10.0.404`.

## Configuración y arranque con Docker

Desde la raíz del repositorio, copia la plantilla y agrega la clave:

```powershell
Copy-Item .env.example .env
```

Edita `.env` y configura `GEMINI_API_KEY`. `GEMINI_MODEL` puede mantenerse como `gemini-3.5-flash-lite`. No compartas ni agregues `.env` a Git.

Variables principales:

| Variable | Función | Valor predeterminado |
|---|---|---|
| `GEMINI_API_KEY` | Credencial de Gemini, obligatoria para el chat | Sin valor |
| `GEMINI_MODEL` | Modelo de Gemini | `gemini-3.5-flash-lite` |
| `MOCK_API_URL` | URL de la API mock vista desde el backend | `http://mock-api:4000/` |
| `MOCK_API_PORT` | Puerto de la API mock publicado en el host | `4000` |
| `BACKEND_PORT` | Puerto del backend publicado en el host | `5000` |
| `FRONTEND_PORT` | Puerto de Angular publicado en el host | `5173` |
| `BACKEND_API_URL` | URL del backend entregada al navegador | `http://localhost:5000` |

Construye y levanta todo el sistema:

```powershell
docker compose up -d --build
docker compose ps
```

Para detenerlo: `docker compose down`. Para revisar la operación: `docker compose logs --tail 100 backend frontend mock-api`.

## Dónde consultar el sistema

- [Interfaz y chat](http://localhost:5173/)
- [Salud del backend](http://localhost:5000/api/health)
- [Perfiles y roles](http://localhost:5000/api/users)
- [Salud de la API mock](http://localhost:4000/salud)
- [Programas de la API mock](http://localhost:4000/programas)

Los puertos publicados se pueden cambiar en `.env`. Si se cambia el puerto del backend, actualiza también `BACKEND_API_URL`; `MOCK_API_URL` es la dirección interna que usa el backend para alcanzar la API mock.

## Consulta directa del backend

No hay que iniciar sesión. Obtén los perfiles disponibles y envía un ID existente:

```powershell
Invoke-RestMethod http://localhost:5000/api/users
$body = @{ userId = "EST-001"; message = "¿Cuál es mi promedio ponderado?" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/chat/messages -ContentType "application/json" -Body $body
```

La respuesta exitosa incluye `answer`, `sources` y `provider`. La guía [docs/backend-endpoints.md](docs/backend-endpoints.md) describe también las respuestas de error y los permisos.

## Pruebas

```powershell
npm --prefix frontend ci
npm --prefix frontend run build
npm --prefix frontend test
dotnet test backend/TutorFlow.sln
docker compose --profile test run --build --rm docker-e2e
```

Las pruebas locales de Angular y .NET no consumen cuota Gemini. El runner E2E llama al backend integrado y hace una consulta real al proveedor; requiere `.env` con una clave válida y puede consumir cuota. Las pruebas optativas de integración .NET con Gemini se habilitan mediante `RUN_GEMINI_INTEGRATION_TESTS=true`.
