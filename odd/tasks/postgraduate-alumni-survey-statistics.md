# Alumni survey statistics report migration
Migrate `EstadisticaEncuestaAlumni.aspx` to `/postgrado/estadisticas-encuesta-alumni`, preserving its static 30-question dictionary and export-only 32-column output. Delivery: `feat/postgraduate-alumni-survey-statistics`; no commits until explicit user approval.

## Specs
S1. "Informes/Postgrado/EstadisticaEncuestaAlumni.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Decisions
- Preserve export-only behavior: the legacy GridView is hidden and survey rows are not shown on screen.
- Preserve the visible 30-question dictionary using the shared `app-table`.
- Preserve the 32 exported columns: `Cod_Alumno`, 30 question fields and `Fecha`; filename stem `EncuestaAlumniPostgrado`, exported as XLSX.
- No filters: the legacy screen has none.
- Keep the SP as is; `P10` remains its raw value because the SP does not translate it, even though `P11`–`P13` do.
- Permission: `REPORT_VIEW_POSTGRADUATE_ALUMNI_SURVEY_STATISTICS`.

## Tasks
T1 [S1] DONE: typed SP contract, query, permission, download endpoint and workbook; 3 focused tests GREEN.
T2 [S1] DONE: Angular route/page with shared dictionary and export; 29 focused UI tests GREEN.
T3 [S1,S2] DONE: documentation, full suites/build and final report; .NET 511, Node 38, Angular 280/285, build GREEN.

## Log
L1. Informes/Postgrado/EstadisticaEncuestaAlumni.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from clean master after the ISS merge; created feat/postgraduate-alumni-survey-statistics. Legacy: static 30-question dictionary, no filters, hidden 32-column grid exported as EncuestaAlumniPostgrado.xls, SP spRec_EstadisticaEncuestaAlumni.
L4. Backend and UI complete. Focused .NET 3/3 and UI 29/29 GREEN. Documentation added.
L5. Full .NET 511/511 and Node 38/38 GREEN; Angular 280/285 with the five known Node 26 localStorage failures; Vite build GREEN with the known bundle/CommonJS warnings. `git diff --check` clean. No commits or staging.
L6. User requested the shared search: added `app-search` filtered through `app-table.searchTerm`; focused UI 30/30 GREEN and Vite build GREEN. No commit.
