# Postgraduate course activities report migration
Migrate `ActividadesCursosPostgrado.aspx` to `/postgrado/actividades-cursos`, preserving its year filter, 22-column grid, search/sort and Excel export. Delivery: `feat/postgraduate-course-activities`; no commits until explicit user approval.

## Specs
S1. "Informes/Postgrado/ActividadesCursosPostgrado.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Decisions
- Preserve the year filter and the 22 columns, including `Id` and the raw suspension date.
- The legacy loads the default year automatically; the new page loads it on entry and reloads when pressing Mostrar.
- Use the shared `app-table` and `app-search`; one grid, no tabs.
- Export XLSX with filename `Reporte Actividades Cursos Postgrado - {year}.xlsx`.
- Years use `spRec_Años(NO, Todos)`; the report uses `spRec_ReporteActividadesCursosPostgrado(@Anno)`.
- Permission: `REPORT_VIEW_POSTGRADUATE_COURSE_ACTIVITIES`.
- Do not modify the SP or schema.

## Tasks
T1 [S1] DONE: typed SP, filters, query, permission, report/download endpoints and workbook; 3 focused tests GREEN.
T2 [S1] DONE: Angular route/page with year filter, shared table/search and export; 33 focused UI tests GREEN.
T3 [S1,S2] DONE: documentation, full suites/build and final report; .NET 522, Node 38, Angular 296/301, build GREEN.

## Log
L1. Informes/Postgrado/ActividadesCursosPostgrado.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from clean master; created feat/postgraduate-course-activities. Legacy: year filter, 22-column grid, auto-bind on entry, export filename "Reporte Actividades Cursos Postgrado - {year}.xls", SP spRec_ReporteActividadesCursosPostgrado.
L4. Backend and UI complete. Focused .NET 3/3 and UI 33/33 GREEN. Documentation added.
L5. Full .NET 522/522 and Node 38/38 GREEN; Angular 296/301 with the five known Node 26 localStorage failures; Vite build GREEN with the known bundle/CommonJS warnings. `git diff --check` clean. No commits or staging.
