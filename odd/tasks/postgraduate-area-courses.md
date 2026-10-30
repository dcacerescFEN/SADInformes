# Postgraduate area courses migration
Migrate `Informes/Postgrado/ReporteAreasCatedras.aspx` ("Reporte Cátedras Áreas") to `/postgrado/alumnos-por-area`, preserving the two-year filter, 28 legacy columns and Excel export. Delivery: `feat/postgraduate-area-courses`; no commits until explicit user approval.

## Specs
S1. "ahora migrar Informes/Postgrado/ReporteAreasCatedras.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Tasks
T1 [S1] DONE: typed `spRec_CatedrasAreas_ReportePostgrado` contract, two-year catalog/query, permission, report/download endpoints and 28-column XLSX; 19 focused tests GREEN. Route: inline. Commit: withheld.
T2 [S1] DONE: Angular route/page/catalogue entry with two year filters, Mostrar, 28-column table and shown-selection export; 21 focused UI tests GREEN. Route: inline. Commit: withheld.
T3 [S1,S2] DONE: documentation, README/permission matrix, full suites/build and diff check; 439 .NET, 38 Node, 229/234 Angular, build GREEN. Route: inline. Commit: withheld.

## Decisions
- Preserve the two independent year filters (`Año1`, `Año2`) from `spRec_Años(NO, Todos)`; both are required, may be equal, and must exist in the catalog before the SP call.
- Keep the legacy initial hidden behavior but add an explicit **Mostrar** button instead of relying on AutoPostBack.
- Render all 28 legacy columns in the original grid order inside a horizontally scrollable table, with no search or pagination because the legacy screen has neither.
- Export the shown rows with the legacy 28 headers, using `TextWorkbook`; no photos or extra columns.
- Do not rewrite or "fix" the complex SP. Preserve its row order, formatted teaching average, observations and `Confirmado` text.
- Use a report-specific read permission; no Keycloak changes, data writes or process restarts.

## Log
L1. ahora migrar Informes/Postgrado/ReporteAreasCatedras.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from master de35ed3 (teacher validities merged); created feat/postgraduate-area-courses. Legacy inspected: two year dropdowns, 28 columns, export AlumnosPregradoPostgrado.xls, permission ver Postgrado + cuadro de honor.

L4. T1 complete: result, wrapper, AreaCourses Application module, permission, filters/report/download and 28-column Excel. Focused 19 GREEN; architecture 4/4. No commits.

L5. T2 complete: route `/postgrado/alumnos-por-area`, catalogue available, two year ng-selects, Mostrar, 28-column horizontal table and shown-selection Excel export. Focused UI 21 GREEN. No commits.

L6. T3 complete: docs/area-courses.md, README and permission row. Full .NET 439/439, Node 38/38, Angular 229/234 (same five Node26/localStorage failures), production build GREEN, diff check clean. No independent verifier; no live SQL Server/Excel/browser proof. No commits/staging.

L7. Runtime bugfix: SQL returns `Creditos` as int, but the result mapped it to decimal?, causing InvalidCastException on materialization. Changed result to int?, added an isolated ADO materialization test with `Creditos` typeof(int). Focused AreaCourses 20/20 GREEN. No commits.

L8. UX addition: Search de SADAlumnos over all 28 visible fields and 10-row Bootstrap pagination (±2 with first/last and `...`); page resets on search and year change. Export remains the complete shown selection. Focused UI 6/6 GREEN. No commits.
