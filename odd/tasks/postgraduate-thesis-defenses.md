# Postgraduate thesis defenses migration
Migrate `Informes/Postgrado/ReporteProfesoresNuevo.aspx` ("Reporte Defensas de Tesis Postgrado") to `/postgrado/defensas-tesis`, preserving the four `Todos`-aware filters, two result grids and two Excel downloads. Delivery: `feat/postgraduate-thesis-defenses`; no commits until explicit user approval.

## Specs
S1. "ahora migrar Informes/Postgrado/ReporteProfesoresNuevo.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Tasks
T1 [S1] DONE: typed GCI/GCI2/catalogue SP contracts, filters/query, permission, report endpoints and two Excel writers; 16 focused tests GREEN. Route: inline fallback. Commit: withheld.
T2 [S1] DONE: Angular page with four Todos filters, cascading modalities, both result grids and two downloads; 24 focused UI tests GREEN. Route: inline. Commit: withheld.
T3 [S1,S2] DONE: documentation, README/permission matrix, full suites/build and diff check; 473 .NET, 38 Node, 245/250 Angular, build GREEN. Route: inline. Commit: withheld.

## Decisions
- Keep the two materially different result sets: GCI/Coordinación (20 columns) and GCI2/Tesorería (17 columns), with their original order and headers.
- Preserve `Todos`/empty bypass in both SPs: year and type filters accept `Todos`; modality uses `Todos` (display "Todas"). No filter is applied when the selected value is `Todos`.
- Preserve the legacy modality cascade quirk: default type `Todos` yields only modality `Todos`; changing type reloads its modalities and always re-adds `Todos`.
- Both SPs are called with the same four selected values. Keep their distinct row shapes; do not merge them.
- Export sheet names `InformeComision`/`InformeTesorería`; filenames include the four selected values. Use `TextWorkbook`, no NPOI.
- Do not modify the SPs or schema. Document the legacy swallowed exceptions and SQL concatenation as historical defects; the migration uses typed wrappers and parameterized calls.
- Use a report-specific read permission; no Keycloak changes, writes or restarts.

## Log
L1. ahora migrar Informes/Postgrado/ReporteProfesoresNuevo.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from master c02ddc8 (SAD Sigma merged); created feat/postgraduate-thesis-defenses.
L4. Explorer mapped both SPs: GCI 20 columns, GCI2 17, four varchar(100) Todos-aware filters, separate catalogue SPs, two exports and legacy modality-clearing defect. No int/decimal mapping pitfall.

L5. T1 complete: typed wrappers, Todos-aware catalog/query, Coordinación/Tesorería DTOs, permission and both XLSX downloads. Focused 16 GREEN. No commits.

L6. T2 complete: `/postgrado/defensas-tesis`, four ng-selects, type-driven modality reload, Buscar, 20/17-column grids and two exports. Focused UI 24 GREEN after fixing two fixture selectors. No commits.

L7. T3 complete: docs/thesis-defenses.md, README and permission row. Full .NET 473/473, Node 38/38, Angular 245/250 (same five Node26/localStorage failures), production build GREEN, diff check clean. No independent verifier; no live SQL Server/Excel/browser proof. No commits/staging.

L8. UX fix: Año Defensa and Año Ingreso options now explicitly include `Todos`; focused UI 5/5 GREEN. No commits.

L9. UX addition: moved both export buttons to the page header and added independent Search + 10-row Bootstrap pagination (±2 with `...`) to Coordination and Treasury. Focused UI 6/6 GREEN. No commits.

L10. UX addition: Coordinación and Tesorería now render in separate tabs; export buttons remain in the page header. Focused UI 6/6 GREEN. No commits.
