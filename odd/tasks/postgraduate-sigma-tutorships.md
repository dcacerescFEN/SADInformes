# Sigma tutorships report migration
Migrate `Informes/Postgrado/ReporteAyudantiaSigma.aspx` to `/postgrado/ayudantias-sigma`, mapping the SAD view `POSTGRADO_Ayudantes_Sigma_SinHistorial` and enriching 25 columns with Sigma remuneration state. Delivery: `feat/postgraduate-sigma-tutorships`; no commits until explicit user approval.

## Specs
S1. "ahora migrar Informes/Postgrado/ReporteAyudantiaSigma.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Tasks
T1 [S1] DONE: mapped the SAD view, extended Sigma fields, added Application filters/query, permission, endpoints and XLSX; 14 focused tests GREEN. Route: inline. Commit: withheld.
T2 [S1] DONE: 25-column XLSX and Angular page with year/Mostrar/download; 25 focused UI tests GREEN. Route: inline. Commit: withheld.
T3 [S1,S2] DONE: documentation, README/permission matrix, full suites/build and diff check; 487 .NET, 38 Node, 252/257 Angular, build GREEN. Route: inline. Commit: withheld.

## Decisions
- Map only the 20 columns consumed by the legacy SELECT from the SAD view; do not map the whole view.
- Add `EstadoId` and `BanderaId` to the existing `SigmaRemuneracionesContext` minimal mapping; keep `prestacion.cod_imp` as the join key.
- Batch all SAD `COD` values in one Sigma LINQ query, replacing the legacy N+1 loop.
- Preserve the legacy state messages (`PASO 1`..`PAGO`, `ELIMINADA`, `DESCONOCIDO`) using `Math.Floor(estado_id)` and the flag symbol (`⛔`/`🚩`).
- If Sigma is unconfigured or fails, keep the report SAD rows and leave `Existe sigma=NO`, `Fecha sigma`/`Paso`/`Flag`/`id remu` empty; the SAD report remains available.
- Keep the legacy 25-column order and export sheet/name; use `TextWorkbook`, no NPOI.
- Use a report-specific read permission; no writes, Keycloak changes or schema changes.

## Log
L1. ahora migrar Informes/Postgrado/ReporteAyudantiaSigma.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from master a07b568 (thesis defenses merged); created feat/postgraduate-sigma-tutorships.
L4. Explorer mapped the unmodeled SAD view, 20 base columns, 5 Sigma columns, state/flag rules and legacy N+1/exception defects.

L5. T1 complete: keyed minimal view mapping, EstadoId/BanderaId, batched Sigma enrichment with fallback, 25-column DTO/XLSX, endpoints and 14 focused tests GREEN. No commits.

L6. T2/T3 complete: route `/postgrado/ayudantias-sigma`, 25-column table and export; docs and permission row. Full .NET 487/487, Node 38/38, Angular 252/257 (same five baseline localStorage failures), build GREEN, diff check clean. Sigma live connection unavailable; fallback covered. No commits/staging.

L7. UX addition: Search de SADAlumnos over all 25 columns and 10-row Bootstrap pagination (±2 with `...`); page resets on search/year change. Export remains complete shown year. Focused UI 6/6 GREEN. No commits.
