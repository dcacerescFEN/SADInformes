# Inscripción académica: cátedras inscritas sin requisitos

Migrar `SAD/Informes/Inscripcion/InscritasSinRequisitos.aspx`.

## Specs

- **S1:** La ruta `/academic-enrollment/enrolled-without-requirements` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Conservar período y las nueve columnas legadas; los datos provienen de `spRec_InscritasSinRequisitos` y los períodos de `spRec_PeriodosActivosTomaRamos`.
- **S3:** No agregar exportación. Usar `page-header`, `form-section`, `ng-select`, `app-table`, skeleton y búsqueda compartida.

## Tasks

- T1 | S1-S2 | backend: resultado, wrapper, filtros, query y endpoints | completado
- T2 | S1, S3 | frontend: filtro de período, tabla y ruta | completado
- T3 | S1-S3 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «continua con InscritasSinRequisitos.aspx».
L2. Legacy: solo período; SP `spRec_InscritasSinRequisitos`; columnas Código/Nombre/Cátedra/Nombre Cátedra/Sección/Origen/Motivo/Fecha/Usuario.
L3. Implementado `/academic-enrollment/enrolled-without-requirements` con skeleton compartido.
L4. Verificación: backend `EnrolledWithoutRequirementsTests` 3/3 y suite WebApi 562/562; Angular enfocado 12/12; suite completa 371/371; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
