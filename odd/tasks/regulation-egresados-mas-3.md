# Aplicación de reglamento · Egresados más de 3 años

Primer informe de la migración de `Aplicación de reglamento`: `SAD/Informes/AplicacionReglamento/EgresadosMas3.aspx`.

## Specs

S1. El permiso legacy de entrada a la sección es `informes aplicación reglamento`; se migra a `REPORT_VIEW_REGULATION`. La API exige además `REPORT_ACCESS`.

S2. El informe es solo lectura y usa `spRec_AlumnosEgresadosMasTresAños` con el período académico activo, reemplazando `SemestreDefecto`.

S3. La tabla mantiene las columnas legacy: Código, Nombre, Carrera (`Tipo_Alumno`), Decreto, Situación, Egresado de, Semestre de Ingreso y Promoción.

S4. El código del alumno enlaza a la ficha nueva de SADAlumnos. El legacy usaba `alumnos/default.aspx?codigo=<Cod_Alumno>&modo=4`; `modo=4` corresponde a Notas, ruta nueva `students/detail/:id/grade-reports/grades`.

S5. La pantalla se agrega al catálogo, sidebar y rutas de SADInformes bajo `Aplicación de reglamento`.

S6. Sin filtros legacy y sin exportación legacy; se agrega buscador de tabla si mantiene el patrón de las pantallas migradas.

S7. Pruebas backend y Angular; documentación y permisos actualizados.

## Tasks

| ID | Specs | Route | Estado |
| --- | --- | --- | --- |
| T1 | S1 | Permiso `REPORT_VIEW_REGULATION` y equivalencias | done |
| T2 | S2, S3, S7 | Backend SP wrapper, query, endpoint y pruebas | done |
| T3 | S4, S5, S6, S7 | Frontend catálogo, ruta, página, enlace a SADAlumnos y pruebas | done |
| T4 | S1-S7 | Documentación y verificación integral | done |

## Modos legacy investigados

| `modo` legacy | Pantalla legacy | Ruta nueva SADAlumnos |
| --- | --- | --- |
| 4 | Notas | `students/detail/:id/grade-reports/grades` |
| 7 | Avance | `students/detail/:id/grade-reports/progress` |
| 11 | Solicitudes | `students/detail/:id/registration/requests` |
| 34 | Apertura expediente | `students/detail/:id/academic-status/file-opening` |
| 35 | Informe académico comisión | `students/detail/:id/grade-reports/commission-academic-report` |

## Log

L1 (usuario): "los permiso deben seguir el orden de los permisos legacy se necesita pasar los permisos legacy al formato de los nuevos permisos, haz una investigacion de los modos y hacia donde son las redirecciones para verificar las nuevas rutas de sadalumnos , comenzemos por el orden de complejidad comienza con egresadosmas3"

L2 (investigación de modos): modo 4 = Notas → `grade-reports/grades`; modo 7 = Avance → `grade-reports/progress`; modo 11 = Solicitudes → `registration/requests`; modo 34 = Apertura expediente → `academic-status/file-opening`; modo 35 = Informe académico comisión → `grade-reports/commission-academic-report`.

L3 (implementación): permiso `REPORT_VIEW_REGULATION` mapeado desde `informes aplicación reglamento`; backend con SP y período activo; frontend con catálogo, ruta, tabla y enlace a Notas.
