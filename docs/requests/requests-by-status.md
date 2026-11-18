# Solicitudes no impresas — Reconocimiento de Cátedras Aprobadas en Postgrado

Migración inicial de `SAD/Informes/Solicitudes/SolicitudesXEstado.aspx` para el
tipo `67` y estado `NO IMPRESA`.

## Ruta

```text
/requests/non-printed-postgraduate-course-recognition
```

## Filtros

- Carrera: IC, IICG, CA.
- Estado fijo: `NO IMPRESA` (el informe es de no impresas).
- Tipo fijo: `67 · Reconocimiento de Cátedras Aprobadas en Postgrado`.
  No se ofrece selector de tipo.

## Backend

```text
GET /api/reports/requests/by-status/report?career=IC&status=NO%20IMPRESA
```

`spRec_SolicitudesXEstado` recibe siempre el tipo `67` y estado `NO IMPRESA`.

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
