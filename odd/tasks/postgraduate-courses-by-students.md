# Courses vs students
Migrate the legacy postgraduate report using the existing teacher-survey format.
Keep database access read-only and exclude the Todos option.

## Specs
S1. "Continuaremos con la migracion de SAD Informes Postgrado ahora toca migrar Cursos vs Alumnos verifica que necesitas sp, tablas puedes verificar BBDD donde estan los sql o el MappingDB donde estan mapeadas en clases"
S2. "sigue el mismo formato que el informe de Encuenta Docente por curso no incluyas todos"

## Tasks
T1 DONE [S1,S2] Implement read-only report API, SP mapping, exact filters, authorization and tests. Route: inline + independent verification (Risk item 2: report-specific access). Commit: a3f3c49.
T2 DONE [S1,S2] Implement screen, catalogue route, CSV and tests using teacher-survey format. Route: inline. Commit: pending.

## Log
L1. Continuaremos con la migracion de SAD Informes Postgrado ahora toca migrar Cursos vs Alumnos verifica que necesitas sp, tablas puedes verificar BBDD donde estan los sql o el MappingDB donde estan mapeadas en clases
L2. sigue el mismo formato que el informe de Encuenta Docente por curso no incluyas todos
L3. SQL/mapping inspected: main SP filters Alumnos.Sem_Ingreso, not course period; no SQL changes required. Existing SADInformes teacher survey supplies UI/API conventions. T1 in progress; T2 pending.

L4. T1 functional checks: isolated RED (missing report types), GREEN 91/91 .NET HTTP/reader tests. Normal build blocked by running WebApi PID 14312; resolved with --artifacts-path .artifacts/courses, without stopping it. Existing NU1902/NU1903 warnings retained. Native assess unavailable (package-local-binary-missing); independent verifier required for security item 2. T2 UI RED observed (missing component).

L5. T2 implemented: /postgrado/cursos-por-alumnos, teacher-survey layout, entry-semester label, no Todos, sortable table with five legacy-visible columns, CSV BOM/escaping/formula-prefix protection. Angular suite 119/119; focused CSV 9/9; production build passed with initial bundle 732.13 kB versus 500 kB warning. Node structural checks 18/18.
L6. Independent verifier mvs5a0he-2-9u8r: S1 MET, S2 MET, security item 2 no findings; focused .NET 15/15, full .NET 91/91, Angular 119/119 and production build passed. No live database or real-browser check; component DOM tests used. Native assessment/review unavailable (package-local-binary-missing), no native verdict claimed. Backend slice is 556 added lines including tests/docs; retained cohesive authorization/read-path coverage rather than compressing it.
