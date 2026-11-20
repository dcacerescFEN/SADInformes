# Aplicación de reglamento · Ranking de Licenciatura y Egreso

Cuarto informe de la migración de `Aplicación de reglamento`: `SAD/Informes/AplicacionReglamento/Ranking.aspx`.

## Specs

S1. Usa el permiso de sección `REPORT_VIEW_REGULATION`, migrado desde `informes aplicación reglamento`; la API exige además `REPORT_ACCESS`.

S2. El informe es solo lectura y usa `spRec_RankingEgresoXGrupo` con los parámetros `Grupo`, `Periodo`, `RankingPorPeriodo`, `OrdenarPor` y `Orden`.

S3. Los filtros mantienen las opciones legacy: grupo/título, tipo de selección `PROMOCION`/`INGRESO`, período, orden `RANKING`/`NOMBRE` y dirección `ASC`/`DESC`.

S4. Los períodos se cargan desde `spRec_PeriodosAcademicos '00000'` mediante un endpoint de filtros.

S5. La tabla mantiene las columnas legacy: Código, Nombre, Decreto, Promedio, Promoción, Semestre de Ingreso y Lugar (`Ranking de Total`).

S6. El código enlaza a la ficha nueva de SADAlumnos. El legacy usaba `alumnos/default.aspx?codigo=<Cod_Alumno>&modo=7`; `modo=7` corresponde a Avance, ruta nueva `students/detail/:id/grade-reports/progress`.

S7. Sin exportación legacy; buscador de tabla. Pruebas backend y Angular; documentación y verificación integral.

## Tasks

| ID | Specs | Route | Estado |
| --- | --- | --- | --- |
| T1 | S1-S4 | Backend SP wrapper, filtros, query, endpoint y pruebas | done |
| T2 | S3-S7 | Frontend catálogo, ruta, página, filtros, enlace y pruebas | done |
| T3 | S1-S7 | Documentación y verificación integral | done |

## Log

L1 (usuario): "continua con Expedientes Ranking"

L2 (implementación): backend con `spRec_RankingEgresoXGrupo` y filtros de períodos; frontend con filtros legacy, tabla de 7 columnas, `Lugar` como `Ranking de Total` y enlace `modo=7` a `grade-reports/progress`.

L3 (verificación): backend 686/686; Angular 494/494; build correcto.

L4 (corrección): los períodos no aparecían porque el wrapper recibía `@Tipo = NULL`; `spRec_PeriodosAcademicos` necesita `@Tipo = '%'` (y `@Modo = 0`, `@ConTodos = 'NO'`). Se ajustó el handler y se agregó `appendTo="body"` a los `ng-select` para que el desplegable no quede recortado.
