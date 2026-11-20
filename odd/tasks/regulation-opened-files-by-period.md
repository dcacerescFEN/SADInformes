# Aplicación de reglamento · Expedientes abiertos por período

Tercer informe de la migración de `Aplicación de reglamento`: `SAD/Informes/AplicacionReglamento/ExpedientesXPeriodo.aspx`.

## Specs

S1. Usa el permiso de sección `REPORT_VIEW_REGULATION`, migrado desde `informes aplicación reglamento`; la API exige además `REPORT_ACCESS`.

S2. El informe es solo lectura y usa `spRec_ExpedientesAbiertosXPeriodo` con un rango `Desde`/`Hasta`.

S3. La tabla mantiene las columnas legacy: Nombre, Foto, Título/Grado, Rut, Carrera, Situación Académica, Promoción, Fecha Apertura, Teléfono(s), Correo Fen y Correo Personal.

S4. El nombre enlaza a la ficha nueva de SADAlumnos. El legacy usaba `alumnos/default.aspx?modo=34&codigo=<Codigo>`; `modo=34` corresponde a Apertura de expediente, ruta nueva `students/detail/:id/academic-status/file-opening`.

S5. El rango de fechas se captura con dos campos `type="date"` (`yyyy-MM-dd`), se valida que ambos existan y que `Desde <= Hasta`, y se envía al SP en formato ISO.

S6. Sin exportación legacy y con buscador de tabla.

S7. Pruebas backend y Angular; documentación y verificación integral.

## Tasks

| ID | Specs | Route | Estado |
| --- | --- | --- | --- |
| T1 | S1-S3, S5 | Backend SP wrapper, query con rango, endpoint y pruebas | done |
| T2 | S3-S6 | Frontend catálogo, ruta, página, fechas, foto, enlace y pruebas | done |
| T3 | S1-S7 | Documentación y verificación integral | done |

## Log

L1 (usuario): "continua con ExpedientesXPeriodo"

L2 (implementación): backend con rango ISO validado y `spRec_ExpedientesAbiertosXPeriodo`; frontend con formulario Desde/Hasta, carga solo al enviar, 11 columnas, foto por `personCode` y enlace `modo=34` a `academic-status/file-opening`.

L3 (verificación): backend 665/665; Angular 482/482; build correcto.

L4 (corrección): SQL Server interpretaba `yyyy-MM-dd` como `yyyy-dd-MM` en la comparación con `AperturaExpediente.Fecha` (`datetime`) y lanzaba error de conversión. El backend mantiene `yyyy-MM-dd` en el contrato HTTP, pero envía al SP `yyyyMMdd`.
