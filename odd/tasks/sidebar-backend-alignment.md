# Reports navigation and backend alignment
Branch: `feat/reports-navigation-backend`; delivery: feature-branch-chain; base: `c5853080d95503320ec5aef3287698a57ef3b579`.
Checks: Angular/Vitest, Node structural tests, .NET WebApi tests and build; browser proof requires an available browser.

## Specs
S1. "hay que hacer modificaciones la sidebar verificala sadalumnos pero necesito que Postgrado y cada seccion sea un dropwdonw como esta hecho en docentes, tambien hayq eu modificar el backend toma a newsad como base y verifica que cosas faltan por ejmplo no esta scalar, hay cosas que no se ocuapn como AppConfigConfigurationReader, los endpoint de Config no usa el patron mediador"
S2. "Todas las secciones de Informes del SAD"
S3. "Cadena sobre rama de trabajo (recomendado)"

## Tasks
T1 | S1-S2 | route: inline fallback after unavailable worker | done | All 14 section dropdowns and 30 Postgrado cards; focused RED: 5 failed/3 passed, GREEN: 8/8; full Angular: 89/89; build: passed (692.85 kB warning); commit: prepared.
T2 | S1 | route: inline fallback after unavailable worker | pending | Align Config with NewSAD custom mediator, add development-only Scalar/OpenAPI, verify actual configuration usages; Risk: item 5 (package additions); commit: pending.
T3 | S1-S3 | route: checks and independent verifier | pending | Run regression checks, update README and record unavailable browser/native-review evidence; commit: pending.

## Log
L1. User request: "hay que hacer modificaciones la sidebar verificala sadalumnos pero necesito que Postgrado y cada seccion sea un dropwdonw como esta hecho en docentes, tambien hayq eu modificar el backend toma a newsad como base y verifica que cosas faltan por ejmplo no esta scalar, hay cosas que no se ocuapn como AppConfigConfigurationReader, los endpoint de Config no usa el patron mediador"
L2. User selected "Todas las secciones de Informes del SAD" and "Cadena sobre rama de trabajo (recomendado)". Push, PR creation and merge remain unauthorized.
L3. Evidence: docentes uses chevron dropdowns and collapsed flyouts; SADAlumnos uses grouped navigation and flyouts. SADInformes currently has only a Postgrado details element. Legacy Informes has 14 top-level sections.
L4. AppConfigConfigurationReader is NOT unused: registered in Infrastructure/ConfigureServices.cs, consumed by ConfigurationInitialiser and tested by AuthenticationTests. Preserve needed reading and filtering; do not delete live functionality. NewSAD Config uses IRequestDispatcher and GetSiteConfig; its ApiResponse payload and SAD permission policy differ from this application's public contract and must not be copied blindly. Preserve existing menu payload, authentication and error behavior while changing internal dispatch.
L5. Existing uncommitted frontend files are this session's earlier visual Postgrado implementation, not unrelated user edits. T1 includes that cohesive baseline; no history or user work is discarded. Approximate total forecast: 700-1000 authored lines including that baseline, excluding generated package artifacts; review as separate navigation and backend units on the feature branch.
L6. Prior explorer unavailable (missing provider key), writer unavailable before mutation (safe-root bootstrap), and native review assess unavailable (package-local-binary-missing). No browser tool is available. Retry only a valid new route if the environment supports it; never invent verification.
L7. Engram mirror pending: mem_save failed with "unable to open database file (14)". The repository task document is the available durable record.
L8. T1 preserves Inicio, mobile drawer closure and session behavior; all 14 groups are clickable buttons with chevrons, collapsed navigation uses ng-bootstrap body-attached flyouts, and Postgrado expands for its active route. Other groups explicitly show a pending message, not invented business screens. Angular suite: 89/89, build passed; warnings: initial bundle 692.85 kB / 500 kB and TimeoutNaNWarning. Browser proof remains unavailable.
