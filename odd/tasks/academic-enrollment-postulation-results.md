# Inscripción académica: resultados de postulación por curso

Migrar `SAD/Informes/Inscripcion/ResPostulaAgregaXCurso.aspx` en sus dos variantes.

## Specs

- **S1:** Las rutas `/academic-enrollment/postulation-results-by-course` y `/academic-enrollment/add-results-by-course` dejan de ser fichas pendientes y muestran el informe funcional.
- **S2:** Conservar período, rango de proceso, fechas y las 17 columnas legadas; los datos provienen de `spRec_ResPostulaAgregaXCurso`, con períodos de `spRec_PeriodosActivosTomaRamos` y rangos de `spRec_ProcesosPeriodos`.
- **S3:** Soportar `INSCRIPCION` y `AGREGA` mediante el origen de la ruta.
- **S4:** No agregar exportación. Usar `page-header`, `form-section`, `ng-select`, fechas, `app-table` y búsqueda compartida.

## Tasks

- T1 | S1-S2 | backend: resultados, wrappers, filtros, procesos, query y endpoints | completado
- T2 | S1, S3-S4 | frontend: filtros dependientes, fechas y tabla funcional | completado
- T3 | S1-S4 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «ahora migrar Informes/Inscripcion/ResPostulaAgregaXCurso.aspx?origen=INSCRIPCION».
L2. Legacy: período, rango de proceso y fechas; SP `spRec_ResPostulaAgregaXCurso`; 17 columnas visibles. El detalle emergente `ResPostulaAgregaXCursoAlumnos.aspx` queda fuera de esta unidad.
L3. Implementado `/academic-enrollment/postulation-results-by-course` para `INSCRIPCION` y `/academic-enrollment/add-results-by-course` para `AGREGA`, compartiendo el componente.
L4. Verificación: backend `PostulationResultsTests` 4/4 y suite WebApi 547/547; Angular enfocado 13/13; suite completa 355/355; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
