# Inscripción académica: alumnos que completaron libres

Migrar `SAD/Informes/Inscripcion/CompletaronLibres.aspx`.

## Specs

- **S1:** La ruta `/academic-enrollment/students-completed-free-courses` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Conservar período y las cuatro columnas legadas; los datos provienen de `spRec_AlumnosCompletaronLibres` y los períodos de `spRec_PeriodosActivosTomaRamos`.
- **S3:** No agregar exportación. Usar `page-header`, `form-section`, `ng-select`, `app-table` y búsqueda compartida.
- **S4:** La variante de electivos (`CompletaronElectivos.aspx`) no forma parte de esta unidad.

## Tasks

- T1 | S1-S2 | backend: resultado, wrapper, filtros, query y endpoints | completado
- T2 | S1, S3-S4 | frontend: filtro de período, tabla y ruta | completado
- T3 | S1-S4 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «ahora continua con Informes/Inscripcion/CompletaronLibres.aspx».
L2. Legacy: solo período; SP `spRec_AlumnosCompletaronLibres`; columnas Código/Nombre/Curso/Cátedra.
L3. Implementado `/academic-enrollment/students-completed-free-courses`.
L4. Verificación: backend `StudentsCompletedFreeCoursesTests` 3/3 y suite WebApi 550/550; Angular enfocado 12/12; suite completa 358/358; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
