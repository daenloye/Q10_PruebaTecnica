# Validación E2E de preguntas de ejemplo

Fecha: 2026-11-23

SDK: .NET `10.0.404`

Modelo en la prueba en vivo: `gemini-3.5-flash-lite`

## Ejecución anterior (mock fixture)

La prueba `InitialQuestionsE2ETests` cargó los 10 registros de `initial_docs/preguntas_ejemplo.json` como datos de prueba y los envió por el flujo real de login y chat. El backend usó el fixture pasivo `initial_docs/mock-api/datos.json` para construir el contexto y Gemini para responder los casos permitidos. El caso 10 se detuvo con HTTP 403 antes de despachar al agente.

| ID | Rol / usuario | Pregunta | Resultado | HTTP | Cita esperada en el fixture |
|---:|---|---|---|---:|---|
| 1 | Estudiante · EST-001 | Nota mínima para aprobar | PASS | 200 | `reglamento_academico_2025.md · Artículo 5` |
| 2 | Estudiante · EST-001 | Promedio ponderado del periodo | PASS | 200 | `evaluacion_y_calificaciones.md · Secciones 3 y 4` |
| 3 | Estudiante · EST-001 | Faltas en Bases de Datos | PASS | 200 | `Datos del estudiante` |
| 4 | Estudiante · EST-001 | Cálculo de nota definitiva | PASS | 200 | `evaluacion_y_calificaciones.md · Secciones 2 y 3` |
| 5 | Estudiante · EST-004 | Definitiva en Contabilidad | PASS | 200 | `Datos del estudiante + evaluacion_y_calificaciones.md · Sección 3` |
| 6 | Coordinador · COORD-02 | Estudiantes de su programa con asignaturas perdidas | PASS | 200 | `Datos del programa + reglamento_academico_2025.md · Artículo 5` |
| 7 | Estudiante · EST-001 | Horario de atención de Bienestar | PASS | 200 | `bienestar_y_becas.md · Sección 1` |
| 8 | Estudiante · EST-004 | Requisitos de beca por rendimiento | PASS | 200 | `bienestar_y_becas.md · Sección 3.1` |
| 9 | Estudiante · EST-001 | Uso de parqueaderos (no documentado) | PASS · Abstención | 200 | `Sin cita` |
| 10 | Estudiante · EST-001 | Nota de Bases de Datos de Andrés Martínez | BLOQUEADO CORRECTAMENTE | 403 | `reglamento_academico_2025.md · Artículo 22` |

`PASS` confirma que la respuesta cumplió las verificaciones semánticas de los datos esperados y que el backend declaró `rules.md` y `api.md` como fuentes. El caso 9 confirmó abstención ante información ausente. El caso 10 confirmó denegación antes de llamar a Gemini.

La lista `Sources` de la API informa archivos de conocimiento (`rules.md`, `api.md`), no el artículo o sección individual. La columna de cita muestra el valor de referencia del JSON de prueba; los artículos y secciones se incluyen además en las respuestas cuando aplica por instrucción del agente.

## Ajustes derivados de la validación

- La autorización ahora bloquea la consulta de expedientes ajenos tanto por ID como por nombre completo, incluyendo el caso de Andrés Martínez.
- El contexto incluye definitivas y promedio ponderado calculados con la fórmula de `rules.md`; las notas faltantes siguen como pendientes. La asistencia se presenta por separado porque el fixture no identifica excusas aprobadas.
- El prompt pide listar los datos relevantes completos (asignaturas, notas, días, horas y ubicación) y usar los cálculos de TutorFlow como autoritativos.

## Verificación de la solución

- `dotnet build backend/TutorFlow.sln --no-restore`: correcto, cero advertencias y errores.
- `dotnet test backend/TutorFlow.sln` con `RUN_GEMINI_INTEGRATION_TESTS=true`: 12 correctas, 0 fallidas. Incluye la llamada simple real, la suite local y los 10 casos E2E.
- Suite sin la variable opt-in: 10 correctas y las 2 pruebas que consumen cuota omitidas intencionalmente.

## Validación posterior contra la API mock en ejecución

El runner ahora inicia automáticamente `node initial_docs/mock-api/server.js` en un puerto disponible, comprueba `GET /salud`, `GET /programas`, `GET /programas/{id}/estudiantes` y, cuando hay estudiantes, `GET /estudiantes/{id}`, `/notas` y `/asistencia`. El backend usa esa URL HTTP real para el catálogo, los perfiles y el contexto académico. Los casos E2E siguen cargándose dinámicamente desde `preguntas_ejemplo.json`; el runner ya no copia ni lee `datos.json` como fuente directa. El proceso Node arrancado por la prueba se detiene al finalizar.

**Compilación y pruebas locales:** `dotnet build backend/TutorFlow.sln` finalizó sin advertencias ni errores. `dotnet test backend/TutorFlow.sln --no-build` pasó 10 pruebas y omitió las 2 opt-in.

**Diagnóstico y corrección:** se confirmó mediante un test local sin Gemini que la URL del servidor dinámico no se aplicaba al `HttpClient` tipado del backend; el test host conservaba la URL capturada al arrancar `Program`. Se configuró explícitamente `MockAcademicApi` con la URL asignada al proceso Node y se añadió una prueba de regresión que verifica que el contexto recibido por el agente incluya notas, cálculo de promedio y ningún expediente de otro estudiante. Además, se precisó la instrucción del agente para incluir el rango completo de calificación cuando se pregunta el umbral de aprobación.

**Resultado final de la E2E en vivo:** pasó la ejecución única de los diez casos mediante login/chat del backend, usando datos HTTP del proceso Node y Gemini real. Nueve casos cumplieron sus expectativas; el caso 10 se bloqueó antes de Gemini con HTTP 403.

| ID | Rol | Resultado | HTTP |
|---:|---|---|---:|
| 1 | Estudiante | PASS | 200 |
| 2 | Estudiante | PASS | 200 |
| 3 | Estudiante | PASS | 200 |
| 4 | Estudiante | PASS | 200 |
| 5 | Estudiante | PASS | 200 |
| 6 | Coordinador | PASS | 200 |
| 7 | Estudiante | PASS | 200 |
| 8 | Estudiante | PASS | 200 |
| 9 | Estudiante | PASS · abstención | 200 |
| 10 | Estudiante | BLOQUEADO CORRECTAMENTE | 403 |

Durante el diagnóstico, una primera suite conjunta desde el sandbox fue limitada por error TLS hacia Gemini; la ejecución simple con permisos de red autorizados pasó. Verificación final autorizada: `dotnet build backend/TutorFlow.sln` sin advertencias ni errores; `dotnet test backend/TutorFlow.sln --no-build` con 11 aprobadas y 2 opt-in omitidas; suite opt-in completa con `RUN_GEMINI_INTEGRATION_TESTS=true` y `GEMINI_MODEL=gemini-3.5-flash-lite`: 13 aprobadas, 0 fallidas y 0 omitidas. Incluye la prueba de contexto contra la mock API viva, la llamada simple a Gemini y los 10 casos E2E.
