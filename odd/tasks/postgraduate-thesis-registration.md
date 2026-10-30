# Thesis registration report migration
Migrate `RegistroTesisPostgrado.aspx` to `/postgrado/registro-tesis`, preserving its type/modality/year filters and 21-column export-only output. Delivery: `feat/postgraduate-thesis-registration`; no commits until explicit user approval.

## Specs
S1. "ahora migrar Informes/Postgrado/RegistroTesisPostgrado.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Decisions
- Preserve export-only behavior: the legacy GridView is hidden and thesis data is not shown in the UI.
- Preserve the 21 displayed columns and legacy filename stem `RegistroTesisPostgrado`, exported as XLSX.
- Preserve `Todos` in type, modality and year catalogs; the SP converts it to `%`.
- Use `spRec_TiposAlumnoXEscuela(POSTGRADO, SI)` instead of the legacy hardcoded `spRec_TiposAlumno('SI','mvenegas')`; document this as a legacy defect, not a schema change.
- Keep modality dependent on type through `spRec_EncuestaGraduados_Modalidades(tipo)`; years use `spRec_Años(SI, Todos)` as legacy did.
- Do not modify the SP or schema.

## Tasks
T1 [S1] DONE: typed SP contract, dependent catalogs, permission, download endpoint and workbook; 4 focused tests GREEN.
T2 [S1] DONE: Angular route/page with three dependent filters and export; 28 focused UI tests GREEN.
T3 [S1,S2] DONE: documentation, full suites/build and final report; .NET 504, Node 38, Angular 271/276, build GREEN.

## Log
L1. ahora migrar Informes/Postgrado/RegistroTesisPostgrado.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from clean master after the Datos Personales merge; created feat/postgraduate-thesis-registration. Legacy: hidden 21-column grid, type/modality/year filters, SP spRec_RegistroTesisPostgrado; file exported as RegistroTesisPostgrado.xls.
L4. Backend and UI complete. Focused .NET 4/4 and UI 28/28 GREEN. Documentation added.
L5. Full .NET 504/504 and Node 38/38 GREEN; Angular 271/276 with the five known Node 26 localStorage failures; Vite build GREEN with the known bundle/CommonJS warnings. `git diff --check` clean. No commits or staging.
