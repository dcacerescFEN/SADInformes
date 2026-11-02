# AFE progress report migration
Migrate `ReporteAFE.aspx` to `/postgrado/avances-afe`, preserving its cascading filters, AFE task/student/delivery report and Excel export. Delivery: `feat/postgraduate-afe-report`; no commits until explicit user approval.

## Specs
S1. "Informes/Postgrado/ReporteAFE.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Decisions
- Preserve the 12 visible columns and export filename stem `Reporte nivel curso AFE - coordinacion`, now XLSX.
- Filters: period, student type, modality, course/section and advance number. Courses use `spRec_AfePorPeriodoTipoAlumno`; advances come from `DocenciaWeb` AFE tasks.
- Student types use the Postgrado catalog (`spRec_TiposAlumnoXEscuela(POSTGRADO, SI)`) instead of the legacy hardcoded `spRec_TiposAlumno('SI')`.
- Read `DocenciaWeb` through a typed external context and LINQ, never concatenated SQL. The connection is optional at startup; a missing connection returns a sanitized 503.
- The SP `SpRec_ObtenerTareasAFEReporte` supplies task/professor/course metadata; student enrollments come from SAD and deliveries/groups from `DocenciaWeb`.
- Preserve the legacy AFE ordering: newest course task has the highest `OrdenAfe`.
- Use the shared `app-table` and `app-search`; tabs are not needed because there is one report grid.
- Do not modify SPs or schema.
- Permission: `REPORT_VIEW_POSTGRADUATE_AFE_REPORT`.

## Tasks
T1 [S1] DONE: typed SAD SPs, DocenciaWeb context, filters/advances/report, permission, endpoint and Excel; 4 focused tests GREEN.
T2 [S1] DONE: Angular route/page with cascading filters, shared table/search and Excel export; 31 focused UI tests GREEN.
T3 [S1,S2] DONE: documentation, full suites/build and final report; .NET 515, Node 38, Angular 286/291, build GREEN.

## Log
L1. Informes/Postgrado/ReporteAFE.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from clean master; created feat/postgraduate-afe-report. Legacy: five cascading filters, 12-column visible grid, export filename "Reporte nivel curso AFE - coordinacion.xls"; SAD SPs plus DocenciaWeb task tables.
L4. Backend and UI complete. Focused .NET 4/4 and UI 31/31 GREEN. Documentation added.
L5. Full .NET 515/515 and Node 38/38 GREEN; Angular 286/291 with the five known Node 26 localStorage failures; Vite build GREEN with the known bundle/CommonJS warnings. `git diff --check` clean. No commits or staging.
L6. Runtime InvalidCastException Int64→Int32: task IDs are bigint. Restored long on the SP result and DocenciaWeb entities, added a type regression test, and made DI prefer the last context registration. Focused 5/5 and full .NET 516/516 GREEN; Node 38/38. No commit.
L7. A second cast came from spRec_AfePorPeriodoTipoAlumno.Id; changed it to long and extended the regression assertion. Focused 5/5 and full .NET 516/516 GREEN. No commit.
