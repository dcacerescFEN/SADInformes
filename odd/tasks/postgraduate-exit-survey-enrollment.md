# Exit survey enrollment migration
Migrate InscripcionEncuestaSalida.aspx with server-validated, transactional survey enrollment.
Delivery: separate feature branch; no commits until explicit user approval.

## Specs
S1. "continua con la migracion de InscripcionEncuestaSalida.aspx"
S2. "Transacción segura e idempotente" (selected answer: validate all selected rows, skip/reject already enrolled and write nothing if a row fails).
S3. "no hagas un commit espera que yo te de el visto bueno" (standing instruction).

## Tasks
T1 [S1,S2,S3] DONE (27 focused backend tests): Add canonical typed read/write procedure contracts, narrow survey table mapping, filters/list queries and safe batch command with tests. Route: inline; one dependent behavior with backend command and read UI, no independent writer units. Commit: withheld. Risk: item 1 (academic survey writes).
T2 [S1,S2,S3] DONE (HTTP and transaction tests): Add protected read/write endpoints, confirmation flow, screen with filters/photo/ficha/selection and regression tests. Route: inline. Commit: withheld. Risk: item 2 (new report permission).
T3 [S1-S3] PENDING: Document, run applicable suites/build and independent verification for write/transaction and shared surfaces. Route: inline + independent verifier. Commit: withheld.

## Log
L1. continua con la migracion de InscripcionEncuestaSalida.aspx
L2. Legacy read: not SSRS. Filters type (with Todos), modality (with Todos), year NO; list via spRec_EncuestaGraduados_Alumnos; grid columns Tipo,Modalidad,photo,Cod_Persona shown as Rut,Nombre,Cod_Alumno linked,Sem_Ingreso,Fecha_Egreso,Fecha_Titulacion,Inscrito,checkbox,Porcentaje. Inscrito SI rows disable checkbox and show Sin contestar when Porcentaje blank/NULL. Type/modality/year auto-reload list. Excel absent.
L3. Legacy write is unsafe: concatenated SQL, one insert per checked row outside transaction, period taken by splitting Cod_Alumno text on hyphen, date taken from grid text, no duplicate guard, no postcondition, always displays Alumnos Inscritos. App will use typed SP parameters and derive period from validated code suffix, date from server listing (never client), not client text.
L4. User selected Transaccion segura e idempotente: server revalidates all selected codes against the filtered listing inside a Serializable transaction, skips codes already present in Encuesta_Salida_Graduados, inserts only new codes, verifies postcondition, commits all or rolls back everything on any failure. Client cannot send dates, periods or unlisted codes. Concurrent duplicate residual risk exists because table PK includes identity Folio and has no unique Cod_Alumno constraint; serializable read plus postcondition is the strongest application-level guard without schema changes.
L5. Workspace was clean on master ebaacfa; uncommitted ranking work from prior turn is no longer present and will not be restored without a new request. New branch feat/postgraduate-exit-survey-enrollment. No commits/staging.
L6. Canonical contracts: spRec_EncuestaGraduados_TiposAlumno(IncluirTodos,Usuario) includes Todos; spRec_EncuestaGraduados_Modalidades(Tipo) includes Todos; spRec_EncuestaGraduados_Alumnos(Tipo,Modalidad,Ano) uses LIKE replace(Todos,%); spCre_EncuestaGraduados_Ins_Alumno(CodAlumno,Periodo,FechaEgreso varchar and CONVERT103). Preserve Cod_Persona as photo/Rut column and use modern SADAlumnos detail route only when authorized by NavigationService. No SSRS, database DDL, credentials or process restarts.

L7. Implemented typed read/write SP wrappers and local results, minimal survey table mapping, Application filters/list/command, protected endpoints and Angular screen with confirmation/photo/ficha/selection. Focused checks: 27 backend (Application/ADO/HTTP) GREEN after RED compile/absence failures; 6 page + 12 navigation UI tests GREEN. Command uses Serializable, revalidates server listing, skips existing, derives period/date server-side, checks postcondition and rolls back all on failure. No Todos removal here because this SP supports it. No commits/staging. Remaining: full suites/build and independent review of academic write.

L8. Independent mvtplx12-a-1gzj: S1/S2/S3 MET, no candidate-caused blockers, 27 focused independently observed; residual DB uniqueness limitation documented. Advisories addressed: null exit date now passed as NULL rather than empty/1900; code comparisons/dedup use case-insensitive semantics matching Modern_Spanish_CI_AS; GET read failures and POST write failures use distinct sanitized 503 messages; docs updated with exit-survey permissions and 400-vs-409 distinction. Added focused null-date and read/write failure tests. Final .NET308/308 and Node38/38 GREEN; frontend unchanged since full Angular189/194/build GREEN (same five prior Node26/localStorage failures) and no commits/staging. Limits: no live SQL Server serializable/transaction proof, Keycloak assignment, photos/SADAlumnos or real browser. No DB DDL or credentials touched.
