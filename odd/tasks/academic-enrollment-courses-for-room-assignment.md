# Inscripción académica: cursos para asignación de salas

Migrar `SAD/Informes/Inscripcion/CursosSistemaAsignacionSalas.aspx`.

## Specs

- **S1:** La ruta `/academic-enrollment/courses-for-room-assignment` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Filtro de período desde `spRec_PeriodosActivosTomaRamos`; el informe usa `spRec_CursosSistemaAsignacionSalas`.
- **S3:** Conservar las 18 columnas reales del SP: Período, Cátedra, Sección, Eliminado, Clases, Tipo, Vacantes Ofrecidas, Vacantes Ocupadas y Profesor 1 a Profesor 10.
- **S4:** Mantener la exportación XLSX con el nombre `CursosAsignacionSalas<período>.xlsx`.
- **S5:** Usar `page-header`, `form-section`, `ng-select`, `app-table`, skeleton, búsqueda y estado vacío compartidos.

## Tasks

- T1 | S1-S3 | backend: resultado completo, wrapper, query y endpoints | completado
- T2 | S1-S2, S4-S5 | frontend: período, tabla, exportación y ruta | completado
- T3 | S1-S5 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «continua con Informes/Inscripcion/CursosSistemaAsignacionSalas.aspx».
L2. Legacy: solo período por query string; generaba la descarga Excel directamente y no mostraba la tabla.
L3. El modelo scaffolded de MappingDB declaraba solo tres columnas; el SP entrega 18, incluidos Profesor 1 a Profesor 10.
L4. Se agregó visualización en pantalla además de conservar la exportación.
L5. Verificación: backend enfocado 4/4 y suite WebApi 584/584; Angular enfocado 12/12 y suite completa 389/389; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
