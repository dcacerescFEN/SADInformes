# Inscripción académica: resultados de postulación por curso

Migrar `SAD/Informes/Inscripcion/ResPostulaAgregaXCurso.aspx?origen=INSCRIPCION`.

## Specs

- **S1:** La ruta `/academic-enrollment/postulation-results-by-course` deja de ser una ficha pendiente y muestra el informe de postulación.
- **S2:** Conservar período, rango de proceso, fechas y las 17 columnas legadas; los datos provienen de `spRec_ResPostulaAgregaXCurso`, con períodos de `spRec_PeriodosActivosTomaRamos` y rangos de `spRec_ProcesosPeriodos`.
- **S3:** La variante `AGREGA` no forma parte de esta solicitud y permanece pendiente.
- **S4:** No agregar exportación. Usar `page-header`, `form-section`, `ng-select`, fechas, `app-table` y búsqueda compartida.

## Tasks

- T1 | S1-S2 | backend: resultados, wrappers, filtros, procesos, query y endpoints | completado
- T2 | S1, S3-S4 | frontend: filtros dependientes, fechas y tabla funcional | completado
- T3 | S1-S4 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «ahora migrar Informes/Inscripcion/ResPostulaAgregaXCurso.aspx?origen=INSCRIPCION».
L2. Legacy: período, rango de proceso y fechas; SP `spRec_ResPostulaAgregaXCurso`; 17 columnas visibles. El detalle emergente `ResPostulaAgregaXCursoAlumnos.aspx` queda fuera de esta unidad.
L3. Implementado `/academic-enrollment/postulation-results-by-course` para origen `INSCRIPCION`; la variante `AGREGA` conserva su ficha pendiente.
L4. Verificación: backend `PostulationResultsTests` 3/3 y suite WebApi 540/540; Angular enfocado 12/12; suite completa 343/348 con los 5 fallos preexistentes de `localStorage`; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
