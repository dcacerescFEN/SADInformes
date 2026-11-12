# Inscripción académica: obligatorias reprobadas no inscritas

Migrar `SAD/Informes/Inscripcion/ObligatoriasReprobadasNoInscritas.aspx`.

## Specs

- **S1:** La ruta `/academic-enrollment/failed-mandatory-not-enrolled` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Conservar período y las cinco columnas legadas; los datos provienen de `spRec_ObligatoriasReprobadasNoInscritasSinSolicitud` y los períodos de `spRec_PeriodosActivosTomaRamos`.
- **S3:** Mantener visibles las cuatro condiciones de inclusión del texto legado.
- **S4:** No agregar exportación. Usar `page-header`, `form-section`, `ng-select`, `app-table`, skeleton y búsqueda compartida.

## Tasks

- T1 | S1-S2 | backend: resultado, wrapper, filtros, query y endpoints | completado
- T2 | S1, S3-S4 | frontend: filtro de período, condiciones, tabla y ruta | completado
- T3 | S1-S4 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «continua con ObligatoriasReprobadasNoInscritas.aspx».
L2. Legacy: solo período; SP `spRec_ObligatoriasReprobadasNoInscritasSinSolicitud`; columnas Código/Nombre/Tipo/Cátedra/Nombre Cátedra; cuatro condiciones visibles.
L3. Implementado `/academic-enrollment/failed-mandatory-not-enrolled` con skeleton compartido.
L4. Verificación: backend `FailedMandatoryNotEnrolledTests` 3/3 y suite WebApi 565/565; Angular enfocado 13/13; suite completa 375/375; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
