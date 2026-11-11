# Inscripción académica: resultados de permuta por alumno

Migrar `SAD/Informes/Inscripcion/ResPermutaXAlumno.aspx`.

## Specs

- **S1:** La ruta `/academic-enrollment/permutation-results-by-student` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Conservar período, rango de proceso, fechas y las ocho columnas legadas; los datos provienen de `spRec_ResPermutaXAlumno`, con períodos de `spRec_PeriodosActivosTomaRamos` y rangos de `spRec_ProcesosPeriodos`.
- **S3:** No agregar exportación. Usar `page-header`, `form-section`, `ng-select`, fechas, `app-table` y búsqueda compartida.
- **S4:** El origen del proceso es `PERMUTA`.

## Tasks

- T1 | S1-S2 | backend: resultado, wrapper, filtros, procesos, query y endpoints | completado
- T2 | S1, S3-S4 | frontend: filtros dependientes, fechas y tabla funcional | completado
- T3 | S1-S4 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «continua la migracion ahora con Informes/Inscripcion/ResPermutaXAlumno.aspx».
L2. Legacy: período, rango de proceso y fechas; SP `spRec_ResPermutaXAlumno`; ocho columnas.
L3. Implementado `/academic-enrollment/permutation-results-by-student` con origen `PERMUTA`.
L4. Verificación: backend `PermutationResultsTests` 3/3 y suite WebApi 543/543; Angular 351/351 tras el setup de `localStorage`; Node 38/38 tras alinear la versión de EF esperada; build Angular correcto. Sin verificación SQL/navegador real.
