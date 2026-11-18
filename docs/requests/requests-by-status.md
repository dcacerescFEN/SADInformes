# Solicitudes no impresas

Migración de `SAD/Informes/Solicitudes/SolicitudesXEstado.aspx` para solicitudes
no impresas. Cada pantalla tiene un tipo fijo.

## Rutas

```text
/requests/non-printed-postgraduate-course-recognition
/requests/non-printed-seminar-homologation-magister
```

- Reconocimiento de Cátedras Aprobadas en Postgrado: tipo `67`.
- Homologación de Seminario X Electivos Magister: tipo `65`.

## Filtros

- Carrera: IC, IICG, CA.
- Estado fijo: `NO IMPRESA` (el informe es de no impresas).
- Tipo fijo según la ruta: `67` o `65`. No se ofrece selector de tipo.

## Backend

```text
GET /api/reports/requests/by-status/report?career=IC&status=NO%20IMPRESA
```

`spRec_SolicitudesXEstado` recibe el tipo fijo de la ruta y estado `NO IMPRESA`.

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
