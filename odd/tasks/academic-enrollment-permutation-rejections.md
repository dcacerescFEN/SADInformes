# Inscripción académica: permutas rechazadas por vacantes

Migrar `SAD/Informes/Inscripcion/ResPermutaXCursoVacantes.aspx`.

## Specs

- **S1:** La ruta `/academic-enrollment/permutation-rejections-by-course` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Conservar período y las cinco columnas legadas; los datos provienen de `spRec_ResPermutaXCursoVacantes` y los períodos de `spRec_PeriodosActivosTomaRamos`.
- **S3:** Mantener **Rechazos** alineado a la derecha y no agregar exportación.
- **S4:** Usar `page-header`, `form-section`, `ng-select`, `app-table` y búsqueda compartida.

## Tasks

- T1 | S1-S2 | backend: resultado, wrapper, query y endpoints | completado
- T2 | S1, S3-S4 | frontend: filtro de período, tabla y ruta | completado
- T3 | S1-S4 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «ahora continua con Informes/Inscripcion/ResPermutaXCursoVacantes.aspx».
L2. Legacy: solo período; SP `spRec_ResPermutaXCursoVacantes`; cinco columnas con `Rechazos` alineado a la derecha.
L3. Implementado `/academic-enrollment/permutation-rejections-by-course`.
L4. Verificación: backend `PermutationRejectionsTests` 3/3 y suite WebApi 546/546; Angular enfocado 12/12; suite completa 354/354; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
