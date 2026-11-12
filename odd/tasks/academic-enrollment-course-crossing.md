# Inscripción académica: cruce de cursos

Migrar `SAD/Informes/Inscripcion/CruceCursos.aspx`.

## Specs

- **S1:** La ruta `/academic-enrollment/course-crossing` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Filtro de período desde `spRec_PeriodosActivosTomaRamos`; el informe usa `spRec_CruceCursos`.
- **S3:** Conservar las ocho columnas legadas: Curso 1, Cátedra 1, Curso 2, Cátedra 2, Topes IC, Topes IICG, Topes Otros, Topes Total.
- **S4:** Mantener la exportación XLSX de la pantalla legada mediante el `TextWorkbook` compartido.
- **S5:** Usar `page-header`, `form-section`, `ng-select`, `app-table`, skeleton, búsqueda y estado vacío compartidos.

## Tasks

- T1 | S1-S3 | backend: resultado, wrapper, query y endpoints | completado
- T2 | S1-S2, S4-S5 | frontend: período, tabla, exportación y ruta | completado
- T3 | S1-S5 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «continua con Informes/Inscripcion/CruceCursos.aspx».
L2. Legacy: solo período por query string; generaba la descarga Excel directamente y no mostraba la tabla.
L3. Columnas legadas: Curso 1, Cátedra 1, Curso 2, Cátedra 2, Topes IC, Topes IICG, Topes Otros, Topes Total.
L4. Se agregó visualización en pantalla además de conservar la exportación.
L5. Verificación: backend enfocado 4/4 y suite WebApi 580/580; Angular enfocado 12/12 y suite completa 386/386; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
