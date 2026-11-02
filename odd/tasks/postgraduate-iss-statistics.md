# ISS statistics report migration
Migrate `EstadisticasISS/Resumen.aspx` to `/postgrado/estadisticas-iss`, preserving its period/course-section filters, sample table, four question categories and observations. Delivery: `feat/postgraduate-iss-statistics`; no commits until explicit user approval.

## Specs
S1. "continua con Informes/Postgrado/EstadisticasISS/Resumen.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Decisions
- Preserve the five visible sections in one page: Muestra, four category tables and Observaciones. No export, because the legacy screen had none.
- Period catalog uses `spRec_PeriodosAcademicosProgramacion('20040', 0, 'NO', 'POSTGRADO')`; `Todos` is excluded because it produced an empty course-section catalog and a broken default in the legacy screen.
- Course-section catalog is dependent on period through `spRec_EncuestaISSCatedraSeccion(periodo)`.
- Summary uses `spRec_EncuestaISSEstadisticasMuestra`, `spRec_EncuestaISSCategorias`, one `spRec_EvaluacionISSEstadisticas` call per category and `spRec_EvaluacionISSObservaciones`.
- Category descriptions come from a typed table entity for `Evaluacion_ISS_PostgradoCategorias`; no raw SQL.
- Render every category returned instead of the legacy fixed four controls; order follows the SP. Each element (Muestra, category, Observaciones) is a tab.
- Every tab uses the shared `app-table`; percentages and averages are precomputed view fields so the shared table stays generic.
- Do not modify SPs or schema.
- Permission: `REPORT_VIEW_POSTGRADUATE_ISS_STATISTICS`.

## Tasks
T1 [S1] Contract reconstruction, branch and ODD document. Route: inline. Commit: withheld.
T2 [S1] DONE: typed SP contracts, filters, summary, permission and tests; 4 focused tests GREEN. Route: inline (writer unavailable). Commit: withheld.
T3 [S1] DONE: Angular page with dependent filters, sample, category tables and observations; 29 focused UI tests GREEN.
T4 [S1,S2] DONE: documentation, full suites/build and final report; .NET 508, Node 38, Angular 276/281, build GREEN.

## Log
L1. continua con Informes/Postgrado/EstadisticasISS/Resumen.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from clean master; created feat/postgraduate-iss-statistics. Legacy: period/course-section filters, Muestra grid, four category grids with five alternatives plus average, observations grid; five SPs plus category-description table read.
L4. Backend and UI complete. Writer delegation unavailable after three attempts; implemented inline. Focused .NET 4/4 and UI 29/29 GREEN. Documentation added.
L5. Full .NET 508/508 and Node 38/38 GREEN; Angular 276/281 with the five known Node 26 localStorage failures; Vite build GREEN with the known bundle/CommonJS warnings. `git diff --check` clean. No commits or staging.
L6. User requested tabs for each element and the shared table: UI rewritten with dynamic tabs and `app-table`; focused UI 29/29 GREEN.
L7. Full Angular suite 276/281 with the same five known Node 26 localStorage failures; Vite build GREEN. `git diff --check` clean. UI-only change, .NET not rerun. No commits or staging.
L8. User requested hiding tabs without data: tabs are filtered by rows; a single Sin Datos state covers all-empty reports. Focused UI 29/29 GREEN and Vite build GREEN. No commit.
