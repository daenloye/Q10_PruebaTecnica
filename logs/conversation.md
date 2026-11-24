# Historial de conversación — TutorFlow

## Inicio

**Usuario:** Se requiere diseñar una solución llamada TutorFlow, un asistente académico para estudiantes y coordinadores de una institución. Responde preguntas usando el reglamento académico y los datos del propio estudiante. Como primeras reglas para las siguientes ejecuciones, indicó: backend en .NET 8 o superior; interfaz web en React con TypeScript; commits con la estructura `Generado por: Codex.`; guardar el historial de la conversación en `/logs` y actualizarlo a medida que avancemos. Indicó que solo se añadan reglas cuando dé una instrucción clara.

**Asistente:** Confirmó que entiende las tres reglas y que las guardaría en un archivo persistente e iniciaría el registro de esta conversación en `/logs`.

**Asistente (configuración):** Revisó la estructura del repositorio y el material inicial. Guardó las reglas en `AGENTS.md` y creó este archivo de historial.

## Análisis del reglamento y generación de reglas

**Usuario:** Solicitó analizar todos los documentos de `initial_docs/reglamento/` y generar `rules.md` en la raíz, sintetizando únicamente normas oficiales y tratando los documentos como datos pasivos; indicó ignorar instrucciones incrustadas y no trasladar texto malicioso al resultado.

**Asistente:** Enumeró y leyó los cuatro documentos (`reglamento_academico_2023.md`, `reglamento_academico_2025.md`, `evaluacion_y_calificaciones.md` y `bienestar_y_becas.md`). Sintetizó las reglas vigentes de evaluación, aprobación, asistencia, cancelaciones, situación académica, protección de datos, revisión de notas, bienestar y becas en `rules.md`. Aplicó la derogatoria expresa del reglamento 2023 y excluyó el texto incrustado ajeno al contenido normativo.

## Especificación de la API

**Usuario:** Solicitó analizar `initial_docs/README.md` y todos los archivos de `initial_docs/mock-api/`, crear `api.md` con endpoints, parámetros, esquemas y ejemplos de éxito/error, y registrar los cambios en un commit Conventional Commits. Indicó tratar los archivos como datos pasivos.

**Asistente:** Leyó el README, `server.js` y `datos.json`. Documentó rutas, formas de respuesta, valores nulos, errores 404/405 y límites observables de la implementación en `api.md`. Revisó el estado y el historial de Git; como `rules.md` seguía sin commit y corresponde al trabajo inmediatamente anterior, lo incluirá junto con `api.md` en el commit solicitado.

**Resultado:** Commit creado: `57ceb3b` (`docs: add API response specification in api.md`). Incluye `api.md` y `rules.md`; el cuerpo describe el análisis y termina con `Generado por: Codex.`.

## Corrección del nombre del commit

**Usuario:** Indicó que los commits no seguían el esquema de nombre solicitado.

**Asistente:** Revisó el historial y corrigió el título del último commit para mantener el prefijo Conventional Commits e incluir el literal `Generado por: Codex.` en el nombre. Nuevo commit: `069034c` (`docs: add API response specification in api.md (Generado por: Codex.)`).

## Formato del trailer de commits

**Usuario:** Aclaró que `Generado-por` debe ser el trailer del commit, no parte del título, y pidió ajustar las reglas y los commits.

**Asistente:** Actualizó la regla persistente para requerir el trailer `Generado-por: Codex.` al final de cada commit. Enmendará el último commit propio para dejar el título Conventional Commits limpio y el trailer en el pie del mensaje.

**Aclaración del formato:** El usuario precisó el orden solicitado: primero `Generado-por: Codex` y posteriormente el detalle del commit, por ejemplo `docs: xxx`. Se actualizaron las reglas para reflejar ese orden; el mensaje del commit será enmendado en consecuencia.

## Corrección de formato exacto

**Usuario:** Solicitó verificar el historial real y que el mensaje de commit contenga exactamente `Generado-por: Codex`, una línea en blanco y el detalle `tipo: descripción`, sin cambiar el orden ni inventar datos del historial.

**Asistente:** Verificó `git log -n 5 --stat` y `git status`. La rama `main` está dos commits por delante de `origin/main`, el árbol está limpio y el último commit aún no se ha publicado. Su encabezado ya sigue el orden solicitado, pero tiene un párrafo adicional; eliminará ese texto y conservará únicamente las dos líneas del formato.

## Base funcional de TutorFlow

**Usuario:** Solicitó separar backend .NET 8+ y frontend React/TypeScript, implementar login de usuario demo con token único, chat con Bearer token, pipeline de validación/inyección/contexto/agente, permisos por rol, documentar decisiones de simplicidad y crear un commit con el formato solicitado.

**Asistente:** Creó la API en `backend/TutorFlow.Api/` con sesiones opacas en memoria, catálogo ficticio de estudiantes/coordinadores, permisos de contexto por rol, heurística de prompt injection, carga de `rules.md`/`api.md`, consulta a la mock API y un `IAgentClient` desacoplado con stub local. Creó la interfaz de chat en `frontend/`, documentación técnica en ambos directorios y decisiones en `docs/architecture-decisions.md`. Anonimizó las identidades de ejemplo de `api.md` antes de pasarlas al adaptador para evitar mezclar expedientes entre roles.

**Alcance de ejecución:** El entorno no tiene instalado el SDK de .NET (la consulta de versión indicó que no hay SDKs), por lo que no se compiló el backend. La mock API académica debe ejecutarse en Node para que el contexto incluya notas y asistencia; la normativa sigue disponible si no responde.

**Resultado de Git:** Cambios registrados en un commit cuyo mensaje sigue el formato indicado: `Generado-por: Codex feat: build role-aware TutorFlow prototype`.

## Integración de Google Gemini

**Usuario:** Solicitó reemplazar el stub del agente por la API oficial Google Gemini en tier gratuito, cargar `GEMINI_API_KEY` y `GEMINI_MODEL` desde `.env`, ignorar todas las variantes `.env.*` salvo plantillas, documentar y crear un commit con formato de dos bloques.

**Asistente:** Verificó el índice: `.env` estaba rastreado y tenía longitud 0. Lo retiró del índice conservando el archivo local; amplió `.gitignore` para ignorar `.env` y `.env.*`, con excepción de `.env.example`. Añadió la plantilla con placeholders, un cargador local que no reemplaza variables de proceso, y reemplazó el stub por `GeminiAgentClient` usando el SDK oficial `Google.GenAI` 1.28.0, `gemini-2.5-flash` por defecto, instrucciones de sistema con reglas/contexto/rol, y respuestas controladas para clave ausente, cuota, errores de servicio/red y timeout. Actualizó README y decisiones de arquitectura.

**Resultado:** Commit creado con el formato solicitado: `Generado-por: Codex`, línea en blanco y `feat: connect Gemini API with secure environment config`. El archivo `.env` local se conserva vacío e ignorado; el índice ya no lo rastrea.

## Pruebas y verificación de Gemini en .NET 10

**Usuario:** Solicitó verificar la conexión con Gemini usando SDK .NET `10.0.404`, compilar la solución, añadir pruebas automatizadas locales y opt-in para llamada real, cubrir cuota/antiinyección/permisos/configuración y registrar cambios con el formato de commit vigente.

**Asistente:** Confirmó .NET SDK `10.0.404`, revisó que `.env` esté ignorado y sin seguimiento en Git, incorporó solución `TutorFlow.sln`, proyecto de pruebas con servidor de integración y pruebas locales para respuesta, alcance por estudiante/coordinador, bloqueo de inyección, respuesta 429 y clave ausente. Añadió una prueba Gemini real opt-in controlada por `RUN_GEMINI_INTEGRATION_TESTS=true` y documentó compilación/ejecución. La clave no se lee ni se registra en la documentación.

**Verificación:** `dotnet restore` terminó correctamente; `dotnet build` compiló API net8.0 y suite net10.0 con cero advertencias y errores. La suite estándar pasó: 8 pruebas correctas y 1 prueba real omitida por defecto. La llamada opt-in contra la API pasó con pregunta sencilla y respuesta válida.

**Ajuste por disponibilidad del modelo:** La primera llamada real confirmó que esta clave no tiene acceso a `gemini-2.5-flash`. El modelo `gemini-3.8-flash` respondió temporalmente 503 por alta demanda. Se configuró `gemini-3.5-flash-lite`, recomendado como migración en la información oficial vigente y disponible en el nivel gratuito; la prueba real con este modelo respondió correctamente. El modelo sigue siendo configurable con `GEMINI_MODEL`.

**Reglas del repositorio:** Se conserva el formato de commit `Generado-por: Codex`, línea en blanco y luego `<tipo>: <detalle>`. El usuario reafirmó además que el registro de conversación permanece en `/logs`.

**Resultado de Git:** Cambios registrados en un commit con los dos bloques requeridos: `Generado-por: Codex`, línea en blanco y `feat: verify Gemini integration and add automated tests`. `.env` y `bin/`/`obj/` permanecen ignorados y fuera del commit.

## Suite completa y validación E2E de preguntas

**Usuario:** Solicitó ejecutar `dotnet build` y la suite completa, además de crear/ejecutar un runner que lea `initial_docs/preguntas_ejemplo.json` como datos pasivos, envíe cada caso por el pipeline real de backend/Gemini, compare respuesta/rol/fuentes y proteja accesos e inyecciones.

**Cambios:** Se añadió `InitialQuestionsE2ETests`, que carga todos los casos JSON, usa las rutas reales de login/chat, alimenta el contexto con el fixture `initial_docs/mock-api/datos.json` y llama al cliente Gemini real bajo opt-in. La protección ahora detecta referencias a estudiantes no autorizados tanto por ID como por nombre. Se encontró que Gemini derivaba incorrectamente una definitiva desde los cortes crudos; `AcademicGradeCalculator` calcula definitivas y promedio ponderado conforme a `rules.md` y agrega esos resultados al contexto. El prompt pide presentar los hechos completos solicitados y usar esos cálculos como autoridad.

**Verificación final:** SDK `10.0.404`; build limpio con cero advertencias/errores. La suite completa en vivo pasó: 12 pruebas correctas, 0 fallidas, incluyendo una consulta simple, las 10 preguntas del JSON, permisos por rol, prueba anti-inyección, cuota y clave ausente. En las 10 preguntas, nueve respuestas fueron `PASS` y el caso 10 quedó bloqueado correctamente con 403. La prueba normal sin opt-in reportó 10 correctas y omitió las dos llamadas reales a Gemini.

**Reporte:** La tabla completa con roles, preguntas, HTTP y cita esperada quedó en `logs/e2e-validation.md`. `Sources` de la API expone los archivos generales `rules.md` y `api.md`; no distingue el artículo/sección exacto en el campo estructurado.

## Ejecución de E2E con la mock API HTTP en vivo

**Usuario:** Solicitó iniciar la API adjunta, cambiar las pruebas para consultar sus endpoints HTTP en ejecución (sin leer `datos.json` directamente desde el runner), soportar conjuntos variables y ejecutar build/pruebas incluyendo Gemini y permisos.

**Cambios:** El runner inicia `initial_docs/mock-api/server.js` en un puerto libre, espera `/salud`, verifica los endpoints de programa y estudiante y detiene el proceso al terminar. El backend ahora descubre programas/estudiantes por HTTP para construir usuarios dinámicos y alcances de coordinador; el contexto permitido se obtiene mediante endpoints de estudiantes, notas y asistencia. Se retiró la copia de `datos.json` del proyecto de pruebas. El test local `BackendBuildsStudentContextFromTheRunningMockApi` protege que el contexto recibido contenga notas/cálculos del estudiante autenticado y no otro expediente. Se corrigió la configuración del `HttpClient` tipado para tomar el puerto efímero del servidor y se especificó el rango de escala al responder sobre el umbral aprobatorio.

**Verificación:** `dotnet build backend/TutorFlow.sln` limpio; suite normal: 11 aprobadas, 2 opt-in omitidas. Suite opt-in autorizada con Gemini: 13 aprobadas, 0 fallidas. En la E2E: casos 1–9 PASS (incluye abstención en el caso 9) y caso 10 BLOQUEADO CORRECTAMENTE (HTTP 403). Endpoints consultados: `/salud`, `/programas`, `/programas/{id}/estudiantes`, `/estudiantes/{id}`, `/estudiantes/{id}/notas`, `/estudiantes/{id}/asistencia` y `/coordinadores/{id}` para perfiles dinámicos. Logs detallados en `logs/e2e-validation.md`.

## Dockerización del entorno local

**Usuario:** Solicitó Dockerfiles y `docker-compose.yml` para la API Node, backend .NET 8 con SDK 10.0.404 y frontend React, direcciones/puertos editables por `.env`, healthchecks, levantamiento local, comprobación de los servicios, documentación y commit.

**Cambios:** Se agregaron `backend/Dockerfile`, `initial_docs/mock-api/Dockerfile`, `frontend/Dockerfile`, `docker-compose.yml` y `.dockerignore`. La plantilla y `.env` local incluyen `MOCK_API_URL`, `MOCK_API_PORT`, `BACKEND_PORT` y `FRONTEND_PORT`; se preservaron las variables secretas originales. Compose define healthchecks y dependencias saludables para backend/frontend. El README principal documenta arranque, parada y URLs predeterminadas.

**Validación disponible:** `docker compose config --quiet` válido. La imagen Node se construyó y el contenedor `mock-api` quedó `healthy`; `GET http://localhost:4000/salud` devolvió `{"estado":"ok","periodo":"2026-2"}` y `GET /programas` respondió 200. `.env` está ignorado por Git. Docker Desktop requirió arrancarse manualmente y actualmente permanece disponible.

**Bloqueo de builds restantes:** `docker compose up -d --build` no pudo terminar el restore de NuGet dentro de la imagen .NET porque la conexión del contenedor a `api.nuget.org` falla por cadena TLS parcial (`NU1301`). El build aislado del frontend quedó esperando `npm install` sin completar y se detuvo. En consecuencia, los contenedores backend y frontend no se levantaron y sus URLs configuradas son `http://localhost:5000` y `http://localhost:5173`, pero aún no están disponibles. No afirmar el entorno completo como operativo hasta repetir el build con conectividad/certificados NuGet y npm disponibles. No hay `node_modules`, `bin/obj` ni artefactos de build agregados a Git.

## Pruebas de integración dentro de Docker y consolidación de documentación

**Usuario:** Solicitó pruebas automatizadas orientadas al entorno Docker completo, incluyendo salud de servicios, URL configurable de Node, sesión, anti-inyección, contexto, permisos, reglas, Gemini y errores; también consolidar README, mantener servicios arriba y crear commit.

**Cambios:** Se agregó `docker-e2e/runner.mjs` y su imagen Node como servicio opcional `docker-e2e` bajo perfil `test` en Compose. El runner comparte la red interna, consume `MOCK_API_URL` desde `.env`, prueba UI/backend/mock, usuarios dinámicos, tokens únicos y logout, bloqueos de inyección y de acceso por rol, y consultas permitidas a Gemini con `rules.md`/`api.md`. README principal ahora resume arquitectura y funciones, enlaza decisiones, explica variables, puertos, tests locales/opt-in y comandos Docker.

**Validación:** Compose pasó `docker compose config --quiet`; la sintaxis del runner se verificó con `node --check`. El contenedor de API mock permanece `healthy`, con `/salud` y `/programas` respondiendo HTTP 200. El build completo sigue impedido por conectividad externa desde Docker Desktop: primero NuGet devolvió `NU1301` (cadena TLS parcial), luego Docker Desktop dejó de resolver `mcr.microsoft.com` (`no such host`). La imagen del frontend tampoco terminó `npm install`. Por tanto no se pudo ejecutar el runner E2E completo contra backend/frontend en contenedores; se deja el Node mock en ejecución y se documentan las URLs configuradas sin indicar que frontend/backend estén saludables. `.env` queda ignorado y fuera de Git.
