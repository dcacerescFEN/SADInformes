# Inscripción académica: alumnos que completaron electivos

Migrar `SAD/Informes/Inscripcion/CompletaronElectivos.aspx`.

## Specs

- **S1:** La ruta `/academic-enrollment/students-completed-electives` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Conservar período y las seis columnas legadas; los datos provienen de `spRec_AlumnosCompletaronElectivos` y los períodos de `spRec_PeriodosActivosTomaRamos`.
- **S3:** No agregar exportación. Usar `page-header`, `form-section`, `ng-select`, `app-table` y búsqueda compartida.

## Tasks

- T1 | S1-S2 | backend: resultado, wrapper, filtros, query y endpoints | completado
- T2 | S1, S3 | frontend: filtro de período, tabla y ruta | completado
- T3 | S1-S3 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «ahora continua con Informes/Inscripcion/CompletaronElectivos.aspx».
L2. Legacy: solo período; SP `spRec_AlumnosCompletaronElectivos`; columnas Código/Nombre/Curso/Cátedra/Decreto/Tipo Cátedra.
L3. Implementado `/academic-enrollment/students-completed-electives`.
L4. Verificación: backend `StudentsCompletedElectivesTests` 3/3 y suite WebApi 553/553; Angular enfocado 12/12; suite completa 361/361; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
