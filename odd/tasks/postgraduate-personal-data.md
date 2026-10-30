# Personal data report migration
Migrate `DatosPersonalesPostgrado.aspx` to `/postgrado/datos-personales`, preserving its three dependent filters and export-only 26-column Excel output. Delivery: `feat/postgraduate-personal-data`; no commits until explicit user approval.

## Specs
S1. "migrar ahora Informes/Postgrado/DatosPersonalesPostgrado.aspx"
S2. "no hagas un commit espera que yo te de el visto bueno"

## Decisions
- Preserve export-only behavior: the legacy GridView is hidden and personal data is not shown in the UI.
- Preserve `Todos` in type, modality and year catalogs and pass it to the SP, which converts it to `%`.
- Catalogs: `spRec_TiposAlumnoXEscuela(POSTGRADO, SI)`, `spRec_EncuestaGraduados_Modalidades(tipo)` and `spRec_Años(SI, Todos)`.
- Keep the 26 visible columns and legacy filename `Datos_Personales_Postgrado.xlsx`.
- Do not modify the SP or schema.

## Tasks
T1 [S1] DONE: typed SP contract, dependent catalogs, permission, download endpoint and workbook; 4 focused tests GREEN.
T2 [S1] DONE: Angular route/page with dependent filters and export; 27 focused UI tests GREEN.
T3 [S1,S2] DONE: documentation, full suites/build and final report; .NET 500, Node 38, Angular 266/271, build GREEN.

## Log
L1. migrar ahora Informes/Postgrado/DatosPersonalesPostgrado.aspx
L2. Standing instruction retained: no hagas un commit espera que yo te de el visto bueno
L3. Started from master; created feat/postgraduate-personal-data. Legacy: hidden 26-column grid, type/modality/year filters, SP spRec_AlumnosDatosPersonalesPostgrado.

L4. Backend, UI and docs complete. Full .NET 500/500 and Node 38/38 GREEN; Angular 266/271 with the five known Node 26 localStorage failures; Vite build GREEN with the known bundle/CommonJS warnings. `git diff --check` clean. No commits or staging.
