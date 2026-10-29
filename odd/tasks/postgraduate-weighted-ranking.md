# Postgraduate weighted ranking migration
Migrate `RankingAlumnosGraduados.aspx` ("Ranking Notas Ponderado") to `/postgrado/ranking-notas-ponderado`, preserving the legacy stored procedure, filters, positions, averages, percentiles, photos and Excel export. Delivery: `feat/postgraduate-weighted-ranking`; no commits until explicit user approval.

## Specs
S1. "ahora migrar RankingAlumnosGraduados.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Tasks
T1 [S1] DONE: typed `spRec_AlumnosCuadroDeHonorPosgrado` contract, concrete filters/query, permission and read-only endpoints; 24 focused tests GREEN. Route: inline (worker rejected twice). Commit: withheld.
T2 [S1] DONE: native 8-column XLSX download, TextWorkbook shared with SAD-VTI; 27 focused + 21 SAD-VTI tests GREEN. Route: inline. Commit: withheld.
T3 [S1] DONE: Angular route/page/catalogue entry with concrete filters, manual show, photo/ficha behavior, empty/error states and download; 19 focused UI tests GREEN. Route: inline. Commit: withheld.
T4 [S1,S2] Documentation, full suites/build, diff check and final report. Route: inline + verifier if an item is escalated. Commit: withheld.

## Decisions
- Keep `ranking-notas-ponderado` and its existing catalogue entry; mark it available only after the page exists.
- Reuse the ranking conventions already approved for the sibling report: concrete catalog values only (no `Todos`), four dependent filters, query only after **Mostrar**, `Sin Datos` for empty results.
- Do not modify the SP or database schema. The single-row percentile division by `@Alumnos - 1` is an inherited defect; return a sanitized 503 instead of rewriting the formula.
- Use a report-specific read permission; no Keycloak changes or database writes.
- Photos use the fixed legacy host with `perfilNN.jpg` fallback; student links use the existing authorized NavigationService destination when configured, otherwise plain text.

## Log
L1. ahora migrar RankingAlumnosGraduados.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from master 4777462 (EncuestaSalidaGraduados merged); created feat/postgraduate-weighted-ranking. No source changes yet.

L4. T1 implemented inline after two writer-preparation failures. Added result entity, typed wrapper, WeightedRanking Application module, `REPORT_VIEW_POSTGRADUATE_WEIGHTED_RANKING`, `/api/reports/postgraduate/weighted-ranking` filters/report, and 24 focused tests (Application + HTTP permission/no-store/400/503). Architecture 4/4. No commits.

L5. T2 complete: added TextWorkbook and WeightedRankingExcel, refactored SadVtiExcel onto TextWorkbook, added /download with no-store/nosniff and empty-export rejection. Focused 27 GREEN; SAD-VTI 21 GREEN. No commits.

L6. T3 complete: catalogue `ranking-notas-ponderado` marked available, route added, Angular page with four dependent filters, manual Mostrar, 9 visible columns, photo fallback, authorized student link, empty/error and shown-selection Excel. Focused UI 19 GREEN. No commits.

L7. Full checks: .NET 385/385, Node 38/38, Angular 210/215 (same five Node26/localStorage theme/sidebar failures), production build GREEN, diff check clean. Independent verifier pending for shared TextWorkbook refactor and report permission. No commits/staging.

L8. T4 closed with available proof: focused 27 WeightedRanking + 21 SAD-VTI + 19 UI GREEN; full .NET 385/385, Node 38/38, Angular 210/215 (same five baseline localStorage failures), build GREEN, diff check clean. Independent verifier `mvtyve6d-d-5zad` was cancelled by the operator after long runtime; no independent verdict is claimed and it will not be retried. No commits/staging. Limits: no live SQL Server, Keycloak, Excel, browser, photos or SADAlumnos; inherited single-row percentile division by @Alumnos-1 documented, SP unchanged.

L9. UX correction: removed Volver from the weighted-ranking header and added the standard `bi bi-search` icon to Mostrar. Focused UI 5/5 GREEN. No commits.
