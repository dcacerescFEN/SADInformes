# Graduate exit survey statistics migration
Migrate EncuestaSalidaGraduados/Default.aspx and its 14 sections, 8 chart panels and teacher detail as a phased report.
Delivery: dedicated feature branch; no commits until explicit user approval. No implementation assumptions on charts/routes/export until the product decisions below are resolved.

## Specs
S1. "ahora hay que migrar EncuestaSalidaGraduados este son varios elementos verifica que necesitas y crea una rama y tambien un plan por partes"
S2. "no hagas un commit espera que yo te de el visto bueno" (standing instruction).

## Scope discovered
Legacy shell has filters Periodo / Tipo Alumno / Modalidad, a Muestra grid, and postback buttons. It is one page with hidden panels; no per-tab URL. There are 14 ASCX sections and 8 Highcharts panels:
- Likert Q1-Q7: Malla Curricular, Cuerpo Académico, Funcionamiento, Infraestructura, Servicios, Vinculación Medio, Networking.
- Q8 Cátedras x área.
- Q9 teacher ranking (ConocimientoTeorico composes teórico/práctico/formativo).
- Q10 Insatisfacción plus DetalleProfesor drilldown.
- Q11 Conocimientos/habilidades, Q17 Evaluación general, Resumen.
- Chart panels mirror Q1-Q7 and Q17.
All use Periodo/Tipo_Alumno/Modalidad. Legacy type is a hardcoded 13-item list; period and modality come from SPs. Muestra comes from spRec_EncuestaGraduados_Estadisticas; footer totals and Resumen percentages/weighted dimension are computed in page code, not SQL.
Legacy risks to preserve or explicitly decide: dynamic-SQL scalar function with interpolated parameters; empty MappingDB Q result classes; Q9 header/data swap; DetalleProfesor SP filters Modalidad using Tipo_Alumno in one UNION branch and repeats Q13_M2; charts use mixed raw SQL/helpers; no chart library in target.
Permission legacy is "encuesta egresados"; catalogue slug estadisticas-encuesta-salida exists pending.

## Resolved decisions
D1. Chart rendering: native CSS/SVG bars. No Chart.js/ECharts/Highcharts dependency. Preserve stacked alternatives, percentages and weighted dimension; no image/PNG export.
D2. Presentation: single page with tabs; tab may be represented in a query parameter. Keep filters, muestra and active tab state coherent.
D3. Export scope: on-screen only. No PNG, print, CSV or Excel in this migration.
D4. Defect policy: correct the visible labels/data mapping without changing stored procedures, functions, views or DDL. Document Q9 and DetalleProfesor legacy defects. If an SP defect prevents correct display, add a separate read-only Application query rather than silently changing SQL.
D5. Permission mapping: read-only `REPORT_VIEW_POSTGRADUATE_EXIT_SURVEY_STATISTICS`, plus existing REPORT administrator roles. No write permission.

## Phased plan
P0 [decisions]: Resolve D1-D3; record D4-D5 in design; freeze output/rounding parity. No code.
P1 [S1] Shell: permission constant, filters (period/type/modality), Muestra grid with footer totals, route/catalogue activation, tab navigation scaffold and HTTP/UI tests. This unblocks all panels.
P2 [S1] Likert core Q1-Q7: typed SP result/contract, one generic matrix query and shared matrix component; tabs for seven dimensions; SQL/Application/navigation/UI tests. Reuse one query path rather than seven copied handlers.
P3 [S1] Part II/IV: Q8 Cátedras x área and Q11 Conocimientos/habilidades (7 alternatives), typed shapes and tests; independent of P2.
P4 [S1] Part III teachers: Q9 I/II/III ranking, Q10 Insatisfacción and DetalleProfesor drilldown; decide/display legacy defects explicitly; authorization and tests.
P5 [S1] Resumen: dimension percentages and weighted mean from authoritative totals; parity tests for zero rows, rounding and missing alternatives.
P6 [S1] Charts after D1: eight chart panels reusing Q1-Q7/Q17 data; no duplicate data fetching if a section payload is already loaded; accessibility/empty/error states and tests.
P7 [S1] Hardening: docs, full suites/build, live limits, independent verification. No push/PR/merge.
Dependencies: P2 and P3 after P1; P4 after P1; P5 after P2; P6 after P2 and D1. P2-P5 can be committed/reviewed as separate work units when approval is given.

## Task tracking
T0 [D1-D5] DONE: decisions recorded 2026-10-29. Route: user. Commit: n/a.
T1 [S1] DONE (P1 shell focused GREEN): permission, filters, muestra with totals, tabs, route/catalogue, HTTP/UI tests. Route: inline. Commit: withheld.
T2 [S1] DONE (P2 Likert Q1-Q7): typed Q1-Q7 wrappers, generic section query, endpoint and reusable matrix; focused backend/ADO/UI GREEN. Route: inline. Commit: withheld.
T3 [S1] DONE (P3 Q8/Q11 verified): typed Q8/Q11 contracts, queries, endpoints and configurable matrix; docs, focused and full suites observed. Route: inline. Commit: withheld.
T4 [S1] PENDING: P4 docentes/detalle. Route: inline. Commit: withheld.
T5 [S1] PENDING: P5 Resumen. Route: inline. Commit: withheld.
T6 [S1] PENDING: P6 charts. Route: inline + verifier if UI shared surfaces change. Commit: withheld.
T7 [S1] PENDING: P7 hardening/docs/verification. Route: inline + independent verifier. Commit: withheld.

## Log
L1. ahora hay que migrar EncuestaSalidaGraduados este son varios elementos verifica que necesitas y crea una rama y tambien un plan por partes
L2. Standing user constraint retained: no hagas un commit espera que yo te de el visto bueno.
L3. Repository was clean on branch feat/postgraduate-exit-survey-enrollment at master ebaacfa; created feat/postgraduate-graduates-survey from master. No source changes, no commit.
L4. Explorer mvtro2lj-b-y0by inventoried shell, all sections/charts and canonical SPs. Key facts: one ASP.NET page with postback panels; Q1-Q7 share shape; Q8/Q11/Q17 additional shapes; Q9/Q10 teacher paths; Resumen computes percentages/weighted dimension client-side; MappingDB Q shapes unusable; no chart dependency in target; catalogue slug exists but pending.
L5. Plan intentionally phased; no phase implemented before decisions D1-D3 and explicit D4-D5 policy. No database DDL/SP changes are planned. All target queries must be typed/parameterized; no dynamic SQL concatenation copied. Preserve exact visible percentages/rounding unless a decision says otherwise.

L6. Decisions resolved: CSS/SVG native charts; single page with tabs (query-param state allowed); on-screen only (no PNG/print/CSV/Excel); correct visible Q9/detail mapping without database changes; read-only statistics permission. Owner requested branch and plan by parts, so no phase implementation started in this turn. No commits.
L7. Plan startup: P1 shell first. P2 and P3 can proceed independently after P1; P4 after P1; P5 after P2; P6 after P2. P2-P6 each need their own work-unit/approval slice; no single giant commit.

L8. P1 implemented and focused-verified: typed Periodos/Estadisticas/Modalidades wrappers; filters and sample endpoints with view permission/no-store; hardcoded 13-type catalog; Muestra totals; Angular shell with auto loading, dependent modality, stale sample cancellation and 14 placeholder tabs. Checks: 14 backend focused GREEN, 5 component + 12 navigation UI GREEN, route/catalogue active. No commits/staging. P2-P7 remain pending; sections are intentionally placeholders.

L9. P1 closed with available proof: full .NET 293/293, Node 38/38, Angular 188/193 (same five pre-existing Node26/localStorage theme/sidebar failures), production build GREEN, diff check clean. Focused P1: 14 backend, 5 component, navigation test GREEN. No commits/staging. P2 remains pending; no section data is fetched or invented in P1.

L10. P2 implemented: Q1-Q7 typed wrappers share a closed procedure-name helper; generic section query and /likert endpoint; Angular LikertMatrix with five alternatives, per-row percentages, one-decimal es-CL average, footer totals, empty state and tab switching. Backend focused 27 GREEN (including ADO Q1/Q7), component 6 GREEN, navigation 12 GREEN. No commits. Remaining P2 full suites/build verification follows.

L11. P2 closed with available proof: full .NET 306/306, Node 38/38, Angular 189/194 (same five pre-existing Node26/localStorage theme/sidebar failures), production build GREEN, diff check clean. Focused Q1-Q7: 27 backend (including ADO parameters/result Q1/Q7), 6 component, 12 navigation GREEN. No commits/staging. P3 is next; P2 only covers Q1-Q7.

L12. P3 implemented: Q8 five-label and Q11 seven-label typed wrappers/queries/endpoints, MultiScaleMatrix with labels/conteos/porcentajes/promedio/totales and tab loading. Focused backend 31 GREEN including ADO Q8/Q11; component 7 GREEN; navigation remains 12. No commits. Full suites/build pending before closing P3.

L13. P3 closed with available proof: full .NET 310/310, Node 38/38, Angular 190/195 (same five pre-existing Node26/localStorage theme/sidebar failures), production build GREEN, diff check clean. Focused Q8/Q11: 31 backend (including ADO Q8/Q11), 7 component, 12 navigation GREEN. No commits/staging. P4 is next (teachers/insatisfaction/detail); P3 did not add charts or Resumen.
