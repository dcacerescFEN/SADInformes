# Application-owned SAD persistence
Objective: migrate module queries to Application while retaining canonical MappingDB SP execution and generic SadContext transactions.
Branch: refactor/architecture-conventions; delivery strategy: auto-chain (feature-branch-chain, user selected); forecast: 1,200–1,800 authored changed lines; runners: dotnet test/build and node --test.

## Specs
- S1: "la idea seria ocupar la interfaz de ISADContex y asignar las tablas como get y crear un ISADContextProcedures con los procedimiento almacenados y hacer las queries en cada modulo quitando lo de infrastructura genera un plan de como deberia ser"
- S2: "aclaración los sp debe seguir el mismo formato que MappingDb con SqlQeryASync SpCre_MovInscAdministrativo_PostgradoAsync no lo ocupa, para los begin transaction estos deberian estar en SADcontext no hacerlo especifico sino una parte de SadContext puedes verificar si jason taylor tiene alguna implementacion de eso sino implementalo como esta"
- S3: "C:\Users\dcaceresc\source\testing\SADMigrations\MappingDB, leelo"
- S4: "implementa el plan"

## Tasks
- T1 | S1,S4 | inline | done | Expose DbSet getters and move report reads into Application handlers with regression tests | commit: pending.
- T2 | S1-S4 | inline | done | Move enrollment queries into Application, generic transactions into SadContext, and align SP wrappers with MappingDB | Risk: item 4 (transaction/concurrency preservation) | commit: pending.
- T3 | S1-S4 | inline + independent verifier | in_progress | Update conventions/docs; run backend and structural suites, independent verification and native assessment; resolve delivery slicing | commit: pending.

## Log
- L8: Delivery decision: user selected "Cadena sobre rama de la migración (recomendado)". Local review slices: (1) report handlers, report procedure doubles and corresponding docs; (2) enrollment query/context/SP migration, generic transaction tests and final conventions. No push or PR creation authorized. The second slice exceeds the ~400-line advisory because canonical generated SP wrappers and the dependent transaction/query migration need a coherent compilable boundary; no compatibility scaffolding will be added just to reduce counts.
- L9: The first isolated Node parent command ran from the scratch container instead of its work/ subtree and discovered 0 tests (not accepted as proof). Corrected root and copied current candidate API binaries into the isolated default DLL location: all 30 Node tests passed, including the real process smoke test. No frontend sources changed; Angular-specific UI tests/build and live SAD checks were not run.
- L7: Full solution build exited 0; backend suite 131/131; local Node suite 30/30. Independent verifier found no code blockers and passed backend 131/131 in an isolated copy. Its Node run was 29/30 because the process test hardcodes the default DLL location while builds used --artifacts-path; parent will run that process check against the current candidate in the isolated copy. Removed four trailing spaces reported by verifier. Native ASSESS returned package-local-binary-missing/unassessable; item-4 escalation required and received one independent verifier. No native review verdict exists. Dependency NU1902/NU1903 warnings pre-existed.
- L6: T2 structural RED: transaction ownership and canonical SP return tests failed before edits; GREEN: 11 structural tests passed. First C# run caught nullable CancellationToken mismatch in copied wrappers; fixed at call boundary. Enrollment/persistence focused run exited 0. Generic transaction tests exercise real EF relational dispatch over recording ADO.NET, HTTP tests use InMemory plus typed SP doubles; live SAD remains untouched.
- L1: "implementa el plan"
- L2: Authorized prior proposal and clarifications are quoted in S1-S3. Current tree is clean at 626cb3a. Existing branch is refactor/architecture-conventions.
- L3: Canonical MappingDB enrollment SP uses ExecuteSqlRawAsync, typed nullable parameters and optional output return value, not SqlQueryAsync. Jason Taylor has no custom transaction helpers in the inspected current template. Maintain existing Serializable isolation, XACT_ABORT ON, same scoped context, rollback, validation and postconditions. Do not access live SAD.
- L5: T1 structural RED: 2 failures before implementation; GREEN: 2/2 after migration. Report HTTP/mapping regression run exited 0. Python alias unavailable; no Python edits executed; subsequent edits use native tools.
- L4: Inline routing: this is one dependent architectural migration, not independent writer units. Independent verifier required for transaction/concurrency changes. Frontend behavior is unchanged.
