# Inscripción académica: reducción de carga

Migrar `SAD/Informes/Inscripcion/ReduccionCarga.aspx`.

## Specs

- **S1:** La ruta `/academic-enrollment/course-load-reduction` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Filtro de período desde `spRec_PeriodosActivosTomaRamos`; el informe usa `spRec_ReduccionCargas`.
- **S3:** Conservar las once columnas legadas: Período, Cátedra, Sección, Nombre Cátedra, Cód. Alumno, Cód. Persona, Decreto, Nombre, Origen, Tipo Movimiento y Estado.
- **S4:** Mantener la exportación XLSX como `ReduccionCarga<período>.xlsx`.
- **S5:** Usar `page-header`, `form-section`, `ng-select`, `app-table`, skeleton, búsqueda y estado vacío compartidos.

## Tasks

- T1 | S1-S3 | backend: resultado, wrapper, query y endpoints | completado
- T2 | S1-S2, S4-S5 | frontend: período, tabla, exportación y ruta | completado
- T3 | S1-S5 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «continua con Informes/Inscripcion/ReduccionCarga.aspx».
L2. Legacy: solo período por query string; exportaba automáticamente y no tenía filtros visibles.
L3. Columnas legadas: Período, Cátedra, Sección, Nombre Cátedra, Cód. Alumno, Cód. Persona, Decreto, Nombre, Origen, Tipo Movimiento, Estado.
L4. Se agregó visualización en pantalla además de conservar la exportación.
L5. Verificación: backend enfocado 4/4 y suite WebApi 605/605; Angular enfocado 12/12 y suite completa 402/402; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
