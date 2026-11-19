# Solicitudes no impresas

Migración de `SAD/Informes/Solicitudes/SolicitudesXEstado.aspx` para solicitudes
no impresas. Cada pantalla tiene un tipo fijo.

## Rutas

```text
/requests/non-printed-postgraduate-course-recognition
/requests/non-printed-seminar-homologation-magister
/requests/non-printed-other-types
/requests/pending-requests
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
- Pendientes: estado `PENDIENTE` y filtro de tipo con **TODOS** (`%`) por
defecto; al elegir un tipo se envía ese valor a `spRec_SolicitudesXEstado`.

## Backend

```text
GET /api/reports/requests/by-status/types?career=IC&status=PENDIENTE&typeFilter=%25
GET /api/reports/requests/by-status/report?career=IC&status=NO%20IMPRESA&type=OTROS
```

`spRec_SolicitudesXEstado` recibe el tipo fijo de la ruta o el tipo elegido en
Otros Tipos, siempre con estado `NO IMPRESA`.

## Confirmar impresión

En los informes `NO IMPRESA`, el botón **Confirmar impresión** está visible con
`REPORT_PRINT_REQUESTS`. Pide confirmación, envía los códigos visibles a
`POST /api/reports/requests/by-status/confirm-print` y marca cada solicitud con
`spMod_Solicitud_Impresa` dentro de una transacción. Al finalizar recarga el
listado y muestra un toast.

## Editar comentario

La columna **Comentario Validación** se edita inline con permiso
`REPORT_EDIT_REQUEST_VALIDATION`. El botón de edición cambia a guardar, el
comentario se envía a `PUT /api/reports/requests/by-status/comment` y al guardar
vuelve a texto sin recargar la lista. **Cancelar edición** descarta el borrador
sin llamar al backend. Máximo 400 caracteres; vacío guarda `NULL`.

## Imprimir detalle

Con permiso `REPORT_PRINT_REQUESTS`, el botón **Imprimir detalle** abre
`/requests/print-detail` con la carrera, estado y tipo actuales. Ver
[print-detail.md](print-detail.md).

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
