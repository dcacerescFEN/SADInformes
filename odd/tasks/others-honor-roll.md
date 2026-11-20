# Otros · Cuadro de Honor

Informe de la sección **Otros** del menú legacy; el archivo vive en `SAD/Informes/AplicacionReglamento/CuadroHonor.aspx` pero el enlace está en la pestaña Otros.

## Specs

S1. La sección usa `REPORT_VIEW_OTHER` (legacy `informes otros`) y el informe agrega `REPORT_VIEW_HONOR_ROLL` (legacy `cuadro de honor`). La API exige además `REPORT_ACCESS`.

S2. El informe es solo lectura y usa `spRec_AlumnosCuadroDeHonor` con `Carrera`, `AñoIngreso` y `Año`.

S3. Los filtros mantienen las opciones legacy: carrera `IC`/`IICG`/`CA`, año de ingreso y año del cuadro. Ambos años salen de `spRec_Años` con `IncluirTodos='NO'` y `TipoAlumno='Todos'`.

S4. La tabla mantiene las columnas legacy: Posición, Foto, Nombre, Rut, Código Alumno, Semestre de Ingreso, Situación, Promedio y Percentil.

S5. El código enlaza a la ficha nueva de SADAlumnos. El legacy usaba `alumnos/default.aspx?modo=4&codigo=<Código>`; `modo=4` corresponde a Notas, ruta nueva `students/detail/:id/grade-reports/grades`.

S6. Sin exportación legacy; buscador de tabla. Pruebas backend y Angular; documentación y verificación integral.

## Tasks

| ID | Specs | Route | Estado |
| --- | --- | --- | --- |
| T1 | S1-S3 | Permiso, backend SP wrapper, filtros, query, endpoint y pruebas | done |
| T2 | S3-S6 | Frontend catálogo, ruta, página, filtros, foto, enlace y pruebas | done |
| T3 | S1-S6 | Documentación y verificación integral | done |

## Log

L1 (usuario): "continua con CuadroHonor"

L2 (implementación): permiso `REPORT_VIEW_HONOR_ROLL` mapeado desde `cuadro de honor`; backend con `spRec_AlumnosCuadroDeHonor` y filtros de años; frontend en sección Otros con filtros, 9 columnas, foto por Rut y enlace `modo=4` a `grade-reports/grades`.

L3 (verificación): backend 704/704; Angular 506/506; build correcto.

L4 (corrección de ubicación): Cuadro de Honor no está en la pestaña Aplicación de Reglamento del SAD; está en Otros. Se movió el catálogo, la ruta a `/others/honor-roll`, el endpoint a `/api/reports/others/honor-roll` y el permiso de sección a `REPORT_VIEW_OTHER` (legacy `informes otros`).
