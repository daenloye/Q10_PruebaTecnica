# Especificación de la API simulada de TutorFlow

## Ejecución y transporte

- Implementación: `initial_docs/mock-api/server.js`, con datos de `initial_docs/mock-api/datos.json`.
- Requiere Node.js 18 o superior; no usa dependencias externas.
- Iniciar desde `initial_docs/mock-api/` con `node server.js`.
- Puerto predeterminado: `4000`. Se puede configurar mediante la variable de entorno `PORT`.
- Base URL local predeterminada: `http://localhost:4000`.
- Todas las respuestas definidas por la API usan `Content-Type: application/json; charset=utf-8`.
- Solo se admite el método `GET`. No hay parámetros de consulta, cuerpos de solicitud ni encabezados de autenticación definidos por la implementación. Los identificadores se reciben en la ruta y se decodifican como componentes URI.
- Las propiedades no listadas en las respuestas no se devuelven. Los arreglos sin coincidencias para un ID válido se responden como `[]` con estado 200.

## Endpoints

### `GET /salud`

Sin parámetros. Devuelve el estado y el periodo cargado desde los datos.

**200** — objeto:

```json
{
  "estado": "ok",
  "periodo": "2026-2"
}
```

`estado`: string; `periodo`: string.

### `GET /programas`

Sin parámetros. Devuelve todos los programas.

**200** — arreglo de objetos `{ "id": string, "nombre": string, "coordinadorId": string }`.

Ejemplo real:

```json
[
  { "id": "ING-SIS", "nombre": "Ingeniería de Sistemas", "coordinadorId": "COORD-01" },
  { "id": "ADM", "nombre": "Administración de Empresas", "coordinadorId": "COORD-02" }
]
```

### `GET /programas/{id}/estudiantes`

Path: `id` (string), identificador de programa. Devuelve los estudiantes asociados al programa.

**200** — arreglo de objetos `{ "id": string, "nombre": string, "programaId": string, "semestre": number }`.

Ejemplo real para `id=ING-SIS`:

```json
[
  { "id": "EST-001", "nombre": "Laura Gómez", "programaId": "ING-SIS", "semestre": 3 },
  { "id": "EST-002", "nombre": "Andrés Martínez", "programaId": "ING-SIS", "semestre": 3 },
  { "id": "EST-003", "nombre": "Sofía Herrera", "programaId": "ING-SIS", "semestre": 3 }
]
```

Si el programa existe y no tiene estudiantes, la respuesta es `200 []`. Si el programa no existe, devuelve 404.

### `GET /coordinadores/{id}`

Path: `id` (string), identificador de coordinador.

**200** — objeto `{ "id": string, "nombre": string, "programas": string[] }`.

Ejemplo real:

```json
{
  "id": "COORD-01",
  "nombre": "Marcela Ríos",
  "programas": ["ING-SIS"]
}
```

### `GET /estudiantes/{id}`

Path: `id` (string), identificador de estudiante. Devuelve los datos básicos; no incluye notas ni asistencia.

**200** — objeto `{ "id": string, "nombre": string, "programaId": string, "semestre": number }`.

Ejemplo real:

```json
{
  "id": "EST-001",
  "nombre": "Laura Gómez",
  "programaId": "ING-SIS",
  "semestre": 3
}
```

### `GET /estudiantes/{id}/notas`

Path: `id` (string), identificador de estudiante.

**200** — arreglo de objetos con esta estructura:

```json
{
  "estudianteId": "string",
  "codigo": "string",
  "asignatura": "string",
  "creditos": 0,
  "corte1": 0.0,
  "corte2": 0.0,
  "corte3": 0.0
}
```

`creditos`: number; `corte1`, `corte2`, `corte3`: number o `null`. En los datos provistos, los tres cortes están presentes como campos; `corte3` es `null` cuando todavía no tiene nota. La API entrega notas por corte, no calcula definitiva, promedio ni pérdida por inasistencia.

Ejemplo real (incluye el valor pendiente):

```json
[
  {
    "estudianteId": "EST-006",
    "codigo": "EST-103",
    "asignatura": "Estadística",
    "creditos": 4,
    "corte1": 3.9,
    "corte2": 4.1,
    "corte3": null
  }
]
```

### `GET /estudiantes/{id}/asistencia`

Path: `id` (string), identificador de estudiante.

**200** — arreglo de objetos `{ "estudianteId": string, "codigo": string, "asignatura": string, "clasesProgramadas": number, "inasistencias": number | null }`.

Ejemplo real (incluye valor nulo presente en los datos):

```json
[
  {
    "estudianteId": "EST-003",
    "codigo": "ING-204",
    "asignatura": "Inglés III",
    "clasesProgramadas": 32,
    "inasistencias": null
  }
]
```

## Respuestas de error

Todos los errores implementados usan un objeto JSON con un único campo `error` de tipo string.

| Estado | Condición | Cuerpo |
| --- | --- | --- |
| 404 | La ruta no coincide con un endpoint | `{"error":"Ruta no existe"}` |
| 404 | La ruta coincide, pero no existe el estudiante, coordinador o programa solicitado | `{"error":"No encontrado"}` |
| 405 | Método distinto de `GET` | `{"error":"Solo GET"}` |

Ejemplos de cuerpos tal como los genera la API:

```json
{"error":"Ruta no existe"}
```

```json
{"error":"No encontrado"}
```

```json
{"error":"Solo GET"}
```

El servidor no define respuestas 5xx ni un manejador de errores para fallos internos. No debe asumirse un esquema JSON para errores 5xx. Por ejemplo, el código no captura errores de decodificación de URI.

## Consideraciones de integración

- La implementación no valida permisos: expone los datos solicitados y deja el control de acceso a la aplicación que la consuma.
- `GET /estudiantes/{id}/notas` y `GET /estudiantes/{id}/asistencia` comprueban primero que el estudiante exista. Para un ID válido sin registros asociados devuelven `200 []`; para un ID inexistente devuelven 404 `No encontrado`.
- Los campos con `null` son distintos de campos omitidos y deben preservarse al interpretar las respuestas.
- El contenido descrito es una API simulada y los nombres/datos del conjunto de prueba son ficticios.
