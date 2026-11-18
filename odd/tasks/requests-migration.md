# Solicitudes reports migration plan

Migrate the three legacy `SAD/Informes/Solicitudes/*` reports into SADInformes as
the **Solicitudes** section. Read-only investigation only; no implementation,
schema change, stored-procedure change or commit is authorized by this plan.

## Scope

| Legacy page | Target report | Main SP |
|---|---|---|
| `SolicitudesProcesadasAutomaticamente.aspx` | Automatic processing requests | `spRec_SolicitudesProcesadasAutomaticamenteSad` |
| `SolicitudesXEstado.aspx` | Requests by status / non-printed / pending | `spRec_SolicitudesXEstado`, `spRec_SolicitudesXEstado_Tipo` |
| `AlumnosConSolicitudesPermanenciaNoResueltas.aspx` | Students with unresolved permanence requests | `spRec_AlumnosConSolicitudesPermanenciaNoResueltas` |

Target naming:

```text
section: Solicitudes
route prefix: /requests
API prefix: /api/reports/requests
catalogue: src/Web/WebUI/src/app/requests/requests-catalogue.ts
```

The empty `solicitudes` section already exists in
`src/Web/WebUI/src/app/core/layout/report-sections.ts`.

## Legacy behavior and permissions

### Menu visibility

`SAD/Informes/default.aspx` enables the **Solicitudes** tab with
`informes solicitudes` and shows the non-printed/pending links with
`imprimir solicitudes`.

`SolicitudesXEstado.aspx` additionally checks:

```text
imprimir solicitudes
editar comentario validacion solicitudes
```

`consulta alumnos` and `solo informes` are legacy navigation/UI redirects, not
report permissions; do not map them as report permissions.

### Proposed permissions

| Permission | Legacy source | Use |
|---|---|---|
| `REPORT_VIEW_REQUESTS` | `informes solicitudes` | Enter the Solicitudes reports and read data |
| `REPORT_PRINT_REQUESTS` | `imprimir solicitudes` | Print detail and mark requests as printed |
| `REPORT_EDIT_REQUEST_VALIDATION` | `editar comentario validacion solicitudes` | Edit `ComentarioValidacion` |

Keep `REPORT_ACCESS_REPORT_MODULE` as the module-entry permission.

Equivalences to add in `Permisos/Equivalencias-Permisos.md` and descriptions in
`Permisos/Descripciones-Permisos.md`:

```text
informes solicitudes                 > REPORT_VIEW_REQUESTS
imprimir solicitudes                 > REPORT_VIEW_REQUESTS
                                       REPORT_PRINT_REQUESTS
editar comentario validacion solicitudes
                                     > REPORT_VIEW_REQUESTS
                                       REPORT_EDIT_REQUEST_VALIDATION
```

## Report 1: Automatic processing requests

Legacy title: **Solicitudes Procesadas Automáticamente**.

### Filters

- Period: distinct `Solicitudes89.Periodo_Solicitud` where
  `Tipo_Solicitud in ('2','8','10')`, descending.
- Career: `TODAS`, `IC`, `IICG`, `CA`.
- Type: `TODOS`, plus the four legacy labels:
  - Diferir la inscripción de asignaturas reprobadas
  - Inscribir carga superior a la máxima
  - Inscribir junto con requisitos
  - Extensión de plazo obtención título
- Status: `TODAS`, `SIN DATO`, and distinct normalized
  `Tramitacion_Automatizada` values.

### Query

`spRec_SolicitudesProcesadasAutomaticamenteSad`

Legacy passes empty strings for “all”; the SP resolves career/type defaults.
Keep that behavior and pass the selected raw values.

### Columns

`Nombre`, `Código`, `N° Solicitud`, `Fecha`, `Tipo`, `Solicitado`,
`Tram. Admin.`, `Resultado`, `Fecha Proceso`.

- `Código` links to SADAlumnos `/students/detail/{codigo}/registration/requests`.
- `Solicitado` and `Resultado` can contain long text; use multiline cells.

Permission: `REPORT_VIEW_REQUESTS`.

## Report 2: Requests by status

Legacy title: **Solicitudes por Estado** (also used for “No impresas” and
“Pendientes”).

### Filters

- Career: `IC`, `IICG`, `CA`.
- Status: `NO IMPRESA`, `PENDIENTE`.
- Type: options from `spRec_SolicitudesXEstado_Tipo`; `%` means all, `OTROS`
  preserves the legacy special case.

The legacy menu links pre-fill career/status/type through query parameters.
Preserve those entry points with route query parameters or catalogue links.

### Query

`spRec_SolicitudesXEstado(@Carrera, @Estado, @Tipo_Solicitud)`

Legacy hides `Cod_Persona`, `Primero` and `Ultimo`; keep them internal.
`Primero`/`Ultimo` are used to display the request range.

### Columns

`Fecha`, `Solicitud` (`LEFT(Cod_Solicitud,5) + '/' + RIGHT(...)`), `Tipo`,
`Alumno` (link to SADAlumnos), `Foto`, `Situación Académica`,
`Comentario Validación`, `Acciones`.

- Photo: reuse the existing photo URL/probe pattern; `perfilNN.jpg` fallback.
- Comment: editable only with `REPORT_EDIT_REQUEST_VALIDATION`.
- Actions:
  - Print detail (legacy Crystal `ImprimeSolicitud.aspx`).
  - Confirm printed (legacy `spMod_Solicitud_Impresa` per selected row).
- Confirmation is required before marking as printed.

### Comment write

Legacy updates directly:

```sql
UPDATE Solicitudes89
SET ComentarioValidacion = @Comentario
WHERE Cod_Solicitud = @CodSolicitud
```

No schema/SP change is allowed. Add only the minimal `Solicitudes89` mapping
needed for `Cod_Solicitud` and `ComentarioValidacion`, or a typed command in the
existing architecture, and validate the primary key before writing. Preserve
the legacy empty-string-to-null behavior.

### Print blocker

`ImprimeSolicitud.aspx` is a Crystal Reports viewer using
`spRec_SolicitudInscripcion` with `desde`, `hasta`, `tipo` and `carrera`.
SADInformes cannot host Crystal. Before implementing print:

- Confirm an SSRS report equivalent exists and its report path/parameters.
- If it does not exist, deliver read-only + XLSX first and defer print; do not
  invent a Crystal replacement.
- `Confirmar Impresión` / `spMod_Solicitud_Impresa` can still be implemented
  separately behind `REPORT_PRINT_REQUESTS`.

## Report 3: Students with unresolved permanence requests

Legacy title: **Alumnos con Solicitudes de Permanencia No Resueltas**.

### Filters

- Career / `Tipo_Alumno`: `IC`, `IICG`, `CA`.
- Period: legacy uses AppConfig `SemestreDefecto`; decide between an explicit
  period selector and the current academic period. Prefer an explicit period
  filter with a sensible default, matching the rest of SADInformes.

### Main query

`spRec_AlumnosConSolicitudesPermanenciaNoResueltas(@Tipo_Alumno, @Periodo)`

### Nested data

Legacy shows per-student:

- Previous permanence requests (`PERMANENCIA RESUELTAS`).
- Current permanence requests (`PERMANENCIA PENDIENTES`).
- Failed core courses.
- Entry data, credit percentage, performance indicators, semesters, enrolled
  UD, elimination causes, observations and restrictions.

Known SPs:

```text
spRec_AlumnoSolicitudes(@Cod_Alumno, @Filtro)
spRec_AlumnoReprobadasCore(@Cod_Alumno)
```

The repository does not contain definitions for:

```text
spRec_SolicitudesAnteriores
spRec_SolicitudesActuales
spRec_AlumnosReprobadasCore
```

Legacy preloads those three in dictionaries but then still falls back to
per-row `SqlDataSource` binding, so the optimization is ineffective. Resolve
the missing contracts before implementing this report:

1. Obtain the definitions from the target database, or
2. Replace them with one batched read-only query over `Solicitudes89` and
   `Alumnos_Catedras`, or
3. Use the existing per-student SPs with a bounded/cached strategy.

Do not create or modify SPs without explicit authorization.

### UI shape

Use expandable rows or a detail modal rather than forcing every nested list into
one flat table. Keep the main table scannable and expose the nested sections on
demand. Define the XLSX flattening explicitly (one row per student with joined
lists, or separate sheets).

Permission: `REPORT_VIEW_REQUESTS`.

## Cross-cutting implementation

- Add `REQUESTS_REPORTS` to the catalogue and wire the existing
  `solicitudes` section with route prefix `/requests`.
- Add `REPORT_VIEW_REQUESTS` filtering to the sidebar/catalogue.
- Reuse `app-table`/`app-dynamic-table`, `Search`, skeleton and the standard
  `page-header`/`form-section` conventions.
- Use `ng-select` with search and `appendTo="body"` for filters.
- Use `TextWorkbook` for XLSX; filenames must be stable and safe.
- Endpoints return the shared envelope for reads, `204` for writes, sanitized
  `400`/`503` errors and `Cache-Control: no-store` for sensitive data.
- Add focused backend tests for SP parameter mapping, permission boundaries,
  comment update and print marking.
- Add Angular specs for filters, empty/error states, permissions, comment edit
  and export.
- No schema or stored-procedure changes.

## Phases

### T0 — Resolve contracts and decisions

- Confirm missing SP definitions or approve the batched-query alternative.
- Confirm whether an SSRS equivalent of `ImprimeSolicitud` exists.
- Approve the three proposed permissions and legacy equivalences.
- Decide the default period source for the permanence report.

### T1 — Permissions and catalogue

- Add permissions, equivalences and descriptions.
- Add `REQUESTS_REPORTS`, routes and sidebar section.
- Add route/permission regression tests.

### T2 — Automatic processing requests — DONE

- Typed `spRec_SolicitudesProcesadasAutomaticamenteSad` wrapper.
- Query, filters and endpoint.
- Angular report with shared table/search.
- Tests and docs.
- Evidence: WebApi 2 focused tests, Angular 3 focused tests, build GREEN.
- Legacy has no Excel export; no export button was added.

### T3 — Requests by status (read-only) — PARTIAL

- Typed `spRec_SolicitudesXEstado` and `_Tipo` wrappers.
- Filters: career only; type is fixed to `67` and status to `NO IMPRESA`.
- Hidden internal columns, photo/link and empty state.
- Endpoint and Angular reports for type `67` and `65`, both `NO IMPRESA`.
- Legacy has no Excel export.
- Print detail, print confirmation and validation comment remain pending.

### T4 — Validation comments

- Minimal `Solicitudes89` mapping or typed command.
- Permission `REPORT_EDIT_REQUEST_VALIDATION`.
- Confirmation, validation and no-stale-write tests.

### T5 — Print marking and print detail

- `spMod_Solicitud_Impresa` wrapper and command for `REPORT_PRINT_REQUESTS`.
- SSRS print detail only if a validated equivalent exists; otherwise defer.
- Tests for selected rows, confirmation and permission boundaries.

### T6 — Unresolved permanence requests

- Resolve nested-data source.
- Main query and nested detail UI/modal.
- Export strategy and tests.

### T7 — Documentation and full verification

- Docs under `docs/requests/`.
- Update README and permissions docs.
- Run full .NET, Node, Angular and build checks.
- No commit until explicit user approval.

## Definition of done

- All three reports are reachable from **Solicitudes** and respect permissions.
- Legacy filters and visible data are preserved; hidden/internal columns stay
  hidden.
- Missing SP/print decisions are documented, not silently replaced.
- Focused and full suites pass, or unavailable proof is reported as a blocker.
- No schema/SP changes and no commit without explicit approval.
