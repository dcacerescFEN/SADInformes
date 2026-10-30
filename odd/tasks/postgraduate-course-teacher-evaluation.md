# Course/teacher evaluation migration
Migrate `Eva_CursosProfesores.aspx` to `/postgrado/evaluacion-docente-curso`, preserving its year/type filters and export-only Excel behavior. Delivery: `feat/postgraduate-course-teacher-evaluation`; no commits until explicit user approval.

## Specs
S1. "migrar ahora Informes/Postgrado/Eva_CursosProfesores.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Decisions
- Keep export-only behavior: the two legacy grids are hidden and the page only exposes year, type and Export.
- Preserve both SP shapes: `EX` returns the 4-question course shape; `FT` returns the 23-question course shape. They are separate DTOs and Excel layouts.
- Keep `EX` and `FT` labels and the legacy filename `Evaluacion_Docente_Curso.xlsx`.
- Do not modify the SP or database schema.
- Add a report-specific permission and a UI without a raw data table.

## Tasks
T1 [S1] DONE: EX/FT contracts, query, permission, download endpoint and two workbooks; 6 focused tests GREEN.
T2 [S1] DONE: Angular route/page with year/type filters and export; 25 focused UI tests GREEN.
T3 [S1,S2] DONE: documentation, full suites/build and final report; 496 .NET, 38 Node, 261/266 Angular, build GREEN.

## Log
L1. migrar ahora Informes/Postgrado/Eva_CursosProfesores.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from master; created feat/postgraduate-course-teacher-evaluation. Legacy: year + EX/FT type, two hidden export grids, SP spRec_EvaPostgradoCursosDocentes.

L4. EX/FT implementation and UI complete; focused backend 6 and UI 25 GREEN. No commits.

L5. Full checks: .NET 496/496, Node 38/38 (architecture 4/4 after renaming the Executive DTO file to match its record), Angular 261/266 (same five Node26/localStorage failures), production build GREEN, diff check clean. No live SQL Server/Excel/browser proof. No commits/staging.
