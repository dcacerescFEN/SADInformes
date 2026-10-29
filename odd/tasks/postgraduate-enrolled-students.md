# Postgraduate enrolled students migration
Migrate `Informes/Postgrado/AlumnosMatriculados.aspx` ("Alumnos Matriculados MBA") to `/postgrado/alumnos-matriculados`, preserving its export-only behavior and the `spRec_AlumnosMatriculadosPostgrado` contract. Delivery: `feat/postgraduate-enrolled-students`; no commits until explicit user approval.

## Specs
S1. "ahora migrar Informes/Postgrado/AlumnosMatriculados.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Tasks
T1 [S1] DONE: typed `spRec_AlumnosMatriculadosPostgrado` contract, fixed MBA modalities, year catalog, permission, filters/download endpoints; 18 focused tests GREEN. Route: inline. Commit: withheld.
T2 [S1] DONE: Angular route/page/catalogue entry with two filters and direct Excel export; no on-screen PII table; 19 focused UI tests GREEN. Route: inline. Commit: withheld.
T3 [S1,S2] DONE: docs, README/permission matrix, full suites/build and diff check; 403 .NET, 38 Node, 215/220 Angular, build GREEN. Route: inline. Commit: withheld.

## Decisions
- Preserve the legacy export-only screen: `grdCuadro` is `Visible="False"`, so the modern page exposes filters and an Excel download, not a personal-data table.
- Preserve the two hardcoded modalities `FOR THE AMERICAS` and `SUNRISE`; do not replace them with a broader catalog.
- The SP always filters `Tipo_Alumno = 'MBA'`; the year selects `@Anio + 'F'` or `(@Anio + 1) + 'B'` inside SQL. Do not modify the SP or database schema.
- Years come from `spRec_Años(NO, Todos)` and must be four ASCII digits present in the catalog.
- Export the 15 legacy columns in the original order, without photos. Reuse `TextWorkbook`, already shared with SAD–VTI and weighted ranking.
- Use a report-specific read permission; no Keycloak changes, data writes or process restarts.

## Log
L1. ahora migrar Informes/Postgrado/AlumnosMatriculados.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from master 970ef41 (weighted ranking merged); created feat/postgraduate-enrolled-students. Legacy inspected: hidden GridView, hardcoded modalities, export of 15 columns, permission `ver Postgrado` + `cuadro de honor`.

L4. T1 complete: result entity, typed SP wrapper, EnrolledStudents Application module, `REPORT_VIEW_POSTGRADUATE_ENROLLED_STUDENTS`, `/filters` and `/download`, 18 focused Application+HTTP tests GREEN; architecture 4/4. No commits.

L5. T2 complete: route `/postgrado/alumnos-matriculados`, catalogue available, two ng-select filters, direct export with filename `AlumnosMatriculados.xlsx`, Sin Datos on empty 400, no table/preview. Focused UI 19 GREEN after fixing the HttpTesting Blob fixture. No commits.

L6. T3 complete: added docs/enrolled-students.md, README and permission row; full .NET 403/403, Node 38/38, Angular 215/220 (same five Node26/localStorage failures), production build GREEN, diff check clean. No independent verifier requested; no live SQL Server/Excel/browser proof. No commits/staging.
