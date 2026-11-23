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
