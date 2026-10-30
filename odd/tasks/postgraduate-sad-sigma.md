# SAD–Sigma report migration
Migrate `Informes/Postgrado/ReporteVistaSadSigma.aspx` to `/postgrado/vista-sad-sigma`, preserving the year filter, the 37 SQL columns plus the Sigma lookup (`Existe_En_Sigma`, `Creado_En_Sigma`) and Excel export. Delivery: `feat/postgraduate-sad-sigma`; no commits until explicit user approval.

## Specs
S1. "ahora migrar Informes/Postgrado/ReporteVistaSadSigma.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"
S3. "Integración completa con MySQL" — add a MySQL EF context and a configurable connection string, without repository credentials; if Sigma is missing or fails, keep the legacy fallback: `Existe_En_Sigma=NO` and `Creado_En_Sigma` empty.

## Tasks
T1 [S1,S3] DONE: typed SP contract, optional MySQL Sigma lookup with NO/empty fallback, year catalog/query, permission, endpoints and 38-column XLSX; 17 focused tests GREEN. Route: inline. Commit: withheld.
T2 [S1] DONE: Angular route/page/catalogue entry with year filter, Mostrar, 38-column table and shown-selection export; 23 focused UI tests GREEN. Route: inline. Commit: withheld.
T3 [S1-S3] DONE: documentation, README/permission matrix, full suites/build and diff check; 458 .NET, 38 Node, 237/242 Angular, build GREEN. Route: inline. Commit: withheld.

## Decisions
- The SQL SP returns 37 columns including `Existe_En_Sigma` as an empty string. The modern query overrides it to `SI`/`NO`; `Creado_En_Sigma` is appended as the last column, matching the legacy DataTable.
- Sigma lookup uses a configurable `ConnectionStrings:SigmaRemuneraciones`; no credentials are added to the repository. Missing configuration, connection failure or empty COD falls back to `NO`/empty just like the legacy swallowed exception path.
- Batch the MySQL query by all nonempty `COD` values instead of the legacy N+1 loop. Preserve only `cod_imp -> created_at`; no writes.
- Preserve the SP ordering and the 38-column order. Format decimal/date fields invariantly for deterministic XLSX text; do not re-rank or recalculate.
- Add a report-specific read permission and optional MySQL package. No Keycloak changes, credential commits or process restarts.

## Log
L1. ahora migrar Informes/Postgrado/ReporteVistaSadSigma.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. User selected full MySQL Sigma integration with no credentials in the repo and legacy NO/empty fallback.
L4. Started from master 2fc92d9 (area courses merged); created feat/postgraduate-sad-sigma. Legacy inspected: one year AutoPostBack, auto-generated grid, SP spRec_CatedrasAreasXPagosProfesores, per-row MySQL Sigma lookup on COD, export ReporteVistaSadSigma.xls.

L5. T1 complete: optional `ConnectionStrings:SigmaRemuneraciones`, batched lookup by COD through the Sigma context, fallback NO/empty, 37 SQL columns + Creado last, 17 focused tests GREEN. No credentials added. No commits.

L6. T2/T3 complete: route `/postgrado/vista-sad-sigma`, 38-column table with Creado_En_Sigma last, shown-year Excel; docs and permission row. Full .NET 458/458, Node 38/38, Angular 237/242 (same five baseline localStorage failures), build GREEN, diff check clean. Sigma connection is optional and absent in this environment, so live Sigma proof remains unavailable. No commits/staging.

L7. UX addition: Search de SADAlumnos over all 38 visible columns and 10-row Bootstrap pagination (±2 with first/last and `...`); page resets on search and year change; export remains the complete shown year. Focused UI 7/7 GREEN. No commits.

L8. Sigma persistence aligned with AGENTS.md: replaced raw MySqlConnector SQL with `SigmaRemuneracionesContext` and minimal `remuneracion`/`prestacion` mappings plus a LINQ join; switched package to MySql.EntityFrameworkCore 10.0.12. Focused SadSigma 17/17 GREEN. No commits.

L9. Full backend after context refactor: .NET 458/458 GREEN, diff check clean. No live MySQL connection; optional config still absent. No commits.

L10. Conventions applied: table entities moved to `Domain/Entities/SigmaRemuneraciones` as `Remuneracion`/`Prestacion`; `SigmaRemuneracionesContext` is the only persistence class in Infrastructure/Data; `ISigmaRemuneracionesContext` in Application/Common/Interfaces exposes the DbSets and `IsConfigured`; handler runs the LINQ query and the old reader was deleted. AGENTS.md updated. Focused SadSigma 17/17 GREEN. No commits.

L11. Full backend after convention refactor: 457/457 GREEN (reader test removed with the reader), diff check clean. No commits. Sigma live connection still unverified.
