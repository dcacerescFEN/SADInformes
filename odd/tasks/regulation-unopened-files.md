# Aplicación de reglamento · Expedientes no abiertos

Segundo informe de la migración de `Aplicación de reglamento`: `SAD/Informes/AplicacionReglamento/ExpedientesNoAbiertos.aspx`.

## Specs

S1. Usa el permiso de sección `REPORT_VIEW_REGULATION`, migrado desde `informes aplicación reglamento`; la API exige además `REPORT_ACCESS`.

S2. El informe es solo lectura y usa `spRec_ExpedientesNoAbiertos` sin parámetros.

S3. La tabla mantiene las columnas legacy: Nombre, Foto, Título/Grado, Rut, Carrera, Situación Académica, Promoción, Teléfono(s), Correo Fen y Correo Personal.

S4. El nombre enlaza a la ficha nueva de SADAlumnos. El legacy usaba `alumnos/default.aspx?modo=34&codigo=<Codigo>`; `modo=34` corresponde a Apertura de expediente, ruta nueva `students/detail/:id/academic-status/file-opening`.

S5. Se mantiene el aviso legacy: el SAD no valida deudas a nivel de universidad.

S6. Sin filtros legacy, sin exportación legacy y con buscador de tabla.

S7. Pruebas backend y Angular; documentación y verificación integral.

## Tasks

| ID | Specs | Route | Estado |
| --- | --- | --- | --- |
| T1 | S1-S3 | Backend SP wrapper, query, endpoint y pruebas | done |
| T2 | S4-S6 | Frontend catálogo, ruta, página, foto, enlace y pruebas | done |
| T3 | S1-S7 | Documentación y verificación integral | done |

## Log

L1 (usuario): "continua con ExpedientesNoAbiertos"

L2 (implementación): backend `spRec_ExpedientesNoAbiertos` + endpoint `/unopened-files/report`; frontend catálogo/ruta, 10 columnas legacy, foto por `personCode`, enlace `modo=34` a `academic-status/file-opening`, aviso de deudas y buscador.

L3 (verificación): backend 651/651; Angular 470/470; build correcto.
