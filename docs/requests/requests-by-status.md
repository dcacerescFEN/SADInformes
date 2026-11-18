# Solicitudes no impresas

Migración de `SAD/Informes/Solicitudes/SolicitudesXEstado.aspx` para solicitudes
no impresas. Cada pantalla tiene un tipo fijo.

## Rutas

```text
/requests/non-printed-postgraduate-course-recognition
/requests/non-printed-seminar-homologation-magister
/requests/non-printed-other-types
```

- Reconocimiento de Cátedras Aprobadas en Postgrado: tipo `67`.
- Homologación de Seminario X Electivos Magister: tipo `65`.
- Otros Tipos: tipo `OTROS`.

## Filtros

- Carrera: IC, IICG, CA.
- Estado fijo: `NO IMPRESA` (el informe es de no impresas).
- Reconocimiento (`67`) y Homologación (`65`): tipo fijo, sin selector.
- Otros Tipos (`OTROS`): filtro de tipo con **TODOS** por defecto; al elegir un
tipo se envía ese valor a `spRec_SolicitudesXEstado`.

## Backend

```text
GET /api/reports/requests/by-status/types?career=IC
GET /api/reports/requests/by-status/report?career=IC&status=NO%20IMPRESA&type=OTROS
```

`spRec_SolicitudesXEstado` recibe el tipo fijo de la ruta o el tipo elegido en
Otros Tipos, siempre con estado `NO IMPRESA`.

## Tabla

Fecha, Solicitud, Tipo, Alumno, Foto, Situación Académica y Comentario
Validación. El nombre del alumno enlaza a SADAlumnos mediante
`studentsMenuUrl` y la ruta nueva:

```text
/students/detail/<codigo>/registration/requests
```

El legacy no tenía exportación a Excel.

## Pendiente

Impresión detalle (Crystal/SSRS), confirmación de impresión y edición del
comentario de validación quedan para las siguientes fases.
