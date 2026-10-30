# Entry survey statistics migration
Migrate `EstadisticaEncuestaEntrada.aspx` to `/postgrado/estadisticas-encuesta-entrada`, preserving its static question dictionary and export-only raw survey data. Delivery: `feat/postgraduate-entry-survey-statistics`; no commits until explicit user approval.

## Specs
S1. "ahora migrar Informes/Postgrado/EstadisticaEncuestaEntrada.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Tasks
T1 [S1] DONE: typed SP contract, query, permission, report/download endpoints and XLSX; 3 focused tests GREEN.
T2 [S1] DONE: Angular route/page with 40-question dictionary and export; 24 focused UI tests GREEN.
T3 [S1,S2] DONE: documentation, full suites/build and final report; 490 .NET, 38 Node, 257/262 Angular, build GREEN.

## Decisions
- Preserve export-only behavior: the legacy GridView is hidden; do not expose raw personal survey answers in a table.
- Keep the static question dictionary and its code/description text as the visible reference content.
- The SP returns duplicate `Q3_1` and `Q3_2` aliases while the GridView exposes one of each; map the 43 unique visible columns and document the duplicate aliases as an inherited SP defect. Do not modify the SP.
- Export the GridView-visible columns to `EncuestaEntradaPostgrado.xlsx` with `TextWorkbook`.
- Use a report-specific read permission; no writes or schema changes.

## Log
L1. ahora migrar Informes/Postgrado/EstadisticaEncuestaEntrada.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from master; created feat/postgraduate-entry-survey-statistics.
L4. Legacy inspected: static 43-row question dictionary, hidden raw response GridView, SP spRec_EstadisticaEncuestaEntrada, export EncuestaEntradaPostgrado.xls.

L5. T1/T2 complete: 44-column XLSX, export-only UI with 40-question dictionary, permission and route. Focused backend 3 and UI 24 GREEN. No commits.

L6. Full checks: .NET 490/490, Node 38/38, Angular 257/262 (same five Node26/localStorage failures; updated sidebar test to use the catalogue length), production build GREEN, diff check clean. No live SQL Server/Excel/browser proof. No commits/staging.

L7. Runtime duplicate-key fix strategy changed by owner: wrapper reverted to canonical `SqlQueryAsync`; the SP owner will remove duplicate Q3_1/Q3_2 aliases so the SP returns the 44-column shape. No SP change made here. No commits.
