# Inscripción académica: cambios automáticos de mención

Migrar `SAD/Informes/Inscripcion/CambiosMencionAutomaticos.aspx`.

## Specs

- **S1:** La ruta `/academic-enrollment/automatic-mention-changes` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Conservar período y las tres columnas legadas; los datos provienen de `spRec_AlumnosCambioMencionAutomatico` y los períodos de `spRec_PeriodosActivosTomaRamos`.
- **S3:** No agregar exportación. Usar `page-header`, `form-section`, `ng-select`, `app-table`, skeleton y búsqueda compartida.

## Tasks

- T1 | S1-S2 | backend: resultado, wrapper, filtros, query y endpoints | completado
- T2 | S1, S3 | frontend: filtro de período, tabla y ruta | completado
- T3 | S1-S3 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «continua con Informes/Inscripcion/CambiosMencionAutomaticos.aspx».
L2. Legacy: solo período; SP `spRec_AlumnosCambioMencionAutomatico`; columnas Código/Nombre/Mención.
L3. Implementado `/academic-enrollment/automatic-mention-changes` con skeleton compartido.
L4. Verificación: backend `AutomaticMentionChangesTests` 3/3 y suite WebApi 556/556; Angular enfocado 12/12; suite completa 365/365; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
