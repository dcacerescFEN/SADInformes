# Inscripción académica: programación docente por decreto

Migrar `SAD/Informes/Inscripcion/ProgramacionDocenteDecreto.aspx` y sus cuatro variantes de decreto.

## Specs

- **S1:** Las rutas `teaching-schedule-ic-old-plan`, `teaching-schedule-ic-new-plan`, `teaching-schedule-iicg-old-plan` y `teaching-schedule-iicg-new-plan` dejan de ser fichas pendientes.
- **S2:** Un único componente resuelve el decreto y el título desde el slug de la ruta; el backend valida el slug contra un catálogo cerrado.
- **S3:** Filtro de período desde `spRec_PeriodosAcademicosProgramacion`; el informe usa `spRec_ProgramacionDocenteXDecreto` con `PERÍODO` y escuela `%`.
- **S4:** Conservar las nueve columnas legadas: Curso, Cátedra, Nombre, Tipo, UD, Mención, Requisitos, Profesor(es), Clases.
- **S5:** Usar `page-header`, `form-section`, `ng-select`, `app-table`, skeleton, búsqueda y estado vacío compartidos; sin exportación.

## Tasks

- T1 | S1-S4 | backend: resultado, wrapper, catálogo, filtros, query y endpoint | completado
- T2 | S1-S3, S5 | frontend: componente compartido, rutas y catálogo | completado
- T3 | S1-S5 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «continua con Informes/Inscripcion/ProgramacionDocenteDecreto.aspx?decreto=00921/89%».
L2. La página es compartida por cuatro decretos y no tiene exportación.
L3. Columnas legadas: Curso, Cátedra, Nombre, Tipo, UD, Mención, Requisitos, Profesor(es), Clases.
L4. Se corrigió el título de IICG Plan Antiguo: el código legado comparaba `00433/89` sin `%` y caía en el título del plan nuevo.
L5. Verificación: backend enfocado 4/4 y suite WebApi 576/576; Angular enfocado 13/13 y suite completa 383/383; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
