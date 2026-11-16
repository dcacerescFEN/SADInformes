# Solicitudes procesadas automáticamente

Migración de `SAD/Informes/Solicitudes/SolicitudesProcesadasAutomaticamente.aspx`
a `/requests/automatic-processing-requests`.

## Filtros

- Período: períodos distintos de `Solicitudes89` con tipos `2`, `8` y `10`.
- Carrera: `TODAS`, `IC`, `IICG`, `CA`.
- Tipo: `TODOS` y las cuatro etiquetas legacy.
- Resultado: `TODAS`, `SIN DATO` y los valores distintos de
  `Tramitacion_Automatizada`.

## Backend

```text
GET /api/reports/requests/automatic-processing/filters
GET /api/reports/requests/automatic-processing/report
```

Usa `spRec_SolicitudesProcesadasAutomaticamenteSad` y
`REPORT_VIEW_REQUESTS`. Las respuestas de lectura usan el envelope compartido;
los errores son `400`/`503` sanitizados y `Cache-Control: no-store`.

## Tabla

Nombre, Código, N° Solicitud, Fecha, Tipo, Solicitado, Tram. Admin., Resultado y
Fecha Proceso. El código enlaza a SADAlumnos usando `studentsMenuUrl` de la
configuración y agrega `codigo=<alumno>&modo=11` para abrir Solicitudes.

El legacy no tenía exportación a Excel; no se agregó ese botón.

No se modificaron procedimientos ni esquema. No se verificó contra SQL Server
ni navegador real.
