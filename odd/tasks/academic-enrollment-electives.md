# Inscripción académica: electivos sin plan común

Migrar `SAD/Informes/Inscripcion/ElectivosMencionSinPlanComun.aspx` a SADInformes.

## Specs

- **S1:** La ruta `/academic-enrollment/electives-without-common-plan` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Conservar el título legado, el texto explicativo y las columnas `Decreto`, `Código` y `Nombre`; los datos provienen de `spRec_ElectivosDeMencionSinPlanComun` mediante `Decreto2`, `Cod_Catedra` y `Nom_Catedra`.
- **S3:** No agregar filtros ni exportación porque la pantalla legada no los tiene. La carga es automática, con opción de actualizar y estado vacío.
- **S4:** Mantener el enlace en la sidebar de Inscripción académica y el mismo diseño de `page-header`, `form-section`, `app-table` y búsqueda compartida.

## Tasks

- T1 | S1-S2 | backend: resultado del SP, wrapper, query, endpoint y permiso de entrada | completado
- T2 | S1, S3-S4 | frontend: componente funcional, catálogo y ruta | completado
- T3 | S1-S4 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «migrar el informe Informes/Inscripcion/ElectivosMencionSinPlanComun.aspx».
L2. Legacy: carga automática sin filtros ni exportación; SP `spRec_ElectivosDeMencionSinPlanComun`; columnas Decreto/Código/Nombre. Implementado en `/academic-enrollment/electives-without-common-plan` con permiso de entrada `REPORT_ACCESS_REPORT_MODULE`.
L3. Verificación: backend `ElectivesWithoutCommonPlanTests` 2/2 y suite WebApi 535/535; Angular enfocado 44/44; suite completa 335/340 con los 5 fallos preexistentes de `localStorage`; Node 38/38; build WebApi y Angular correctos. Sin verificación SQL/navegador real.
