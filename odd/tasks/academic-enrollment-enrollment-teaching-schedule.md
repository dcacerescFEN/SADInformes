# Inscripción académica: programación docente

Migrar `SAD/Informes/Inscripcion/ProgramacionDocenteCompacta.aspx`.

## Specs

- **S1:** La ruta `/academic-enrollment/enrollment-teaching-schedule` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Filtros Escuela, Período y Área; la coordinación se deriva en backend (`EPOST-TODO` para Postgrado, `%` para Pregrado).
- **S3:** El informe usa `spRec_ProgramacionDocenteResumida` y conserva las 11 columnas legadas, incluida `Rut Profesor`.
- **S4:** Exportación XLSX con el `TextWorkbook` compartido y las mismas filas filtradas.
- **S5:** Usar `page-header`, `form-section`, `ng-select`, `app-table`, skeleton, búsqueda y estado vacío compartidos.

## Tasks

- T1 | S1-S3 | backend: resultados, wrappers, filtros, query y endpoints | completado
- T2 | S1-S2, S4-S5 | frontend: filtros, tabla, exportación y ruta | completado
- T3 | S1-S5 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «continua con la migracion Informes/Inscripcion/ProgramacionDocenteCompacta.aspx».
L2. Legacy: Escuela, Período y Área visibles; coordinación oculta; 11 columnas; exportación Excel.
L3. El SP `spRec_ProgramacionDocenteResumida` entrega `RutProfesor` aunque el modelo scaffolded lo omitía.
L4. No se pudo usar el worker configurado: «Writer repository preparation was unavailable». Se implementó inline como fallback.
L5. Verificación: backend enfocado 7/7 y suite WebApi 572/572; Angular enfocado 13/13 y suite completa 379/379; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
