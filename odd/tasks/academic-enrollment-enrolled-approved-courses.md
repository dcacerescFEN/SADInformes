# Inscripción académica: cátedras inscritas ya aprobadas

Migrar `SAD/Informes/Inscripcion/InscritasYaAprobadas.aspx`.

## Specs

- **S1:** La ruta `/academic-enrollment/enrolled-approved-courses` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Conservar período y las seis columnas legadas; los datos provienen de `spRec_InscritasYaAprobadas` y los períodos de `spRec_PeriodosActivosTomaRamos`.
- **S3:** No agregar exportación. Usar `page-header`, `form-section`, `ng-select`, `app-table`, skeleton y búsqueda compartida.

## Tasks

- T1 | S1-S2 | backend: resultado, wrapper, filtros, query y endpoints | completado
- T2 | S1, S3 | frontend: filtro de período, tabla y ruta | completado
- T3 | S1-S3 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «continua con Informes/Inscripcion/InscritasYaAprobadas.aspx».
L2. Legacy: solo período; SP `spRec_InscritasYaAprobadas`; columnas Código/Nombre/Cátedra/Nombre Cátedra/Sección/Origen.
L3. Implementado `/academic-enrollment/enrolled-approved-courses` con skeleton compartido.
L4. Verificación: backend `EnrolledApprovedCoursesTests` 3/3 y suite WebApi 559/559; Angular enfocado 12/12; suite completa 368/368; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
