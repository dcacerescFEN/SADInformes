# Undergraduate licenciatura report migration
Migrate `LicenciaturaPregrado.aspx` to `/postgrado/proximas-licenciaturas-pregrado`, preserving its on-demand search, 5 columns, search/sort and Excel export. Delivery: `feat/postgraduate-undergraduate-licenciatura`; no commits until explicit user approval.

## Specs
S1. "Informes/Postgrado/LicenciaturaPregrado.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Decisions
- Preserve on-demand behavior: the list loads only after pressing Buscar.
- Preserve the 5 visible columns plus row number: Código Alumno, Nombre, Nombre Carrera, Grado and Estado.
- Use the shared `app-table` and `app-search`; no filters and no tabs because there is one grid.
- Export XLSX with filename stem `ProximasLicenciatura` (legacy `.xls` becomes `.xlsx`).
- Permission: `REPORT_VIEW_POSTGRADUATE_LICENCIATURA`.
- SP `[SAD].[SpList_AlumnosCumpleLicenciatura]` is read as-is; no schema changes.

## Tasks
T1 [S1] DONE: typed SP, query, permission, report/download endpoints and workbook; 3 focused tests GREEN.
T2 [S1] DONE: Angular route/page with Buscar, shared table/search and export; 32 focused UI tests GREEN.
T3 [S1,S2] DONE: documentation, full suites/build and final report; .NET 519, Node 38, Angular 291/296, build GREEN.

## Log
L1. Informes/Postgrado/LicenciaturaPregrado.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from clean master; created feat/postgraduate-undergraduate-licenciatura. Legacy: no filters, Buscar button, 6-column grid (row number + 5 fields), export filename ProximasLicenciatura.xls, SP SAD.SpList_AlumnosCumpleLicenciatura.
L4. Backend and UI complete. Focused .NET 3/3 and UI 32/32 GREEN. Documentation added.
L5. Full .NET 519/519 and Node 38/38 GREEN; Angular 291/296 with the five known Node 26 localStorage failures; Vite build GREEN with the known bundle/CommonJS warnings. `git diff --check` clean. No commits or staging.
L6. Runtime SQL timeout on the expensive licenciatura SP. Added a per-query command timeout parameter that restores the previous value, and applied 300 s only to this SP. Focused 3/3 and full .NET 519/519 GREEN. No commit.
