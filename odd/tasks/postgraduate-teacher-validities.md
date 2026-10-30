# Postgraduate teacher validities migration
Migrate `ProfesoresVigencias.aspx` ("Profesores Confirmados x Cátedra") to `/postgrado/profesores-vigencias`, preserving the year filter, 14 columns, validity state and Excel export. Delivery: `feat/postgraduate-teacher-validities`; no commits until explicit user approval.

## Specs
S1. "ahora migrar ProfesoresVigencias.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Tasks
T1 [S1] DONE: typed `spRec_ProfesoresVigencias` contract, year catalog, query, permission, report/download endpoints and 14-column XLSX; 17 focused tests GREEN. Route: inline fallback (writer bootstrap failed). Commit: withheld.
T2 [S1] DONE: Angular route/page/catalogue entry with year filter, Mostrar, 14-column table, Vigente/No Vigente badges and shown-selection Excel export; 21 focused UI tests GREEN. Route: inline. Commit: withheld.
T3 [S1,S2] DONE: documentation, README/permission matrix, full suites/build and diff check; 420 .NET, 38 Node, 221/226 Angular, build GREEN. Route: inline. Commit: withheld.

## Decisions
- Preserve the year-only filter from `spRec_Años(NO, Todos)`; reject missing, Todos, wildcards, non-four-digit and out-of-catalog years before calling the SP.
- The legacy grid is hidden until a postback. The modern page keeps the initial no-query behavior but adds an explicit **Mostrar** button instead of relying on AutoPostBack.
- Show all 14 legacy columns in a horizontally scrollable table. Render `Vigencia` as `Vigente` (green) or `No Vigente` (red), matching the code-behind.
- Export the same 14 columns and rows as shown, without photos or extra columns. Reuse `TextWorkbook`.
- Do not modify the SP or database schema. Preserve its ordering, LEFT(cu.Periodo,4) year and PENDIENTE fallbacks.
- Use a report-specific read permission; no Keycloak changes, data writes or process restarts.

## Log
L1. ahora migrar ProfesoresVigencias.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from master e2da2ea (enrolled students merged); created feat/postgraduate-teacher-validities. Legacy inspected: year AutoPostBack, grid visible after postback, 14 columns, Vigencia true/false labels, export ProfesoresVigencias.xls, permission ver Postgrado + cuadro de honor.

L4. T1 complete inline after two writer-bootstrap failures. Added result, wrapper, TeacherValidities Application module, permission, filters/report/download, Excel; 17 focused tests GREEN. No commits.

L5. T2 complete: route `/postgrado/profesores-vigencias`, catalogue available, one year ng-select, explicit Mostrar, 14-column scrollable table with green/red validity badges, empty/error states and shown-year Excel export. Focused UI 21 GREEN after fixing two TypeScript fixture issues. No commits.

L6. T3 complete: docs/teacher-validities.md, README and permission row. Full .NET 420/420, Node 38/38, Angular 221/226 (same five Node26/localStorage failures), production build GREEN, diff check clean. No independent verifier; no live SQL Server/Excel/browser proof. No commits/staging.

L7. UX addition: Search de SADAlumnos sobre todas las columnas visibles and 10-row pagination with page reset on search; export remains the complete shown year. Focused UI 7/7 GREEN. No commits.

L8. Pagination format corrected: Bootstrap Anterior/Siguiente, current page ±2 with first/last and `...` for gaps. Focused UI 8/8 GREEN. No commits.
