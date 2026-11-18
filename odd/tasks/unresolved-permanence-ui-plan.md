# Unresolved permanence report — UI adjustment plan

Plan for `AlumnosConSolicitudesPermanenciaNoResueltas` after the first migration.
Read-only investigation; no implementation or commit is authorized by this plan.

## Verified period behavior

The legacy page does **not** normally send an empty period.

Evidence:

```text
SAD/Informes/Solicitudes/AlumnosConSolicitudesPermanenciaNoResueltas.aspx.vb:33
  sqlAlumnosPermanencia.SelectParameters("periodo").DefaultValue =
      ConfigurationManager.AppSettings("SemestreDefecto")

SAD/Web.config:82
  <add key="SemestreDefecto" value="20262" />
```

The SP uses `@Periodo` in two meaningful places:

```sql
dbo.UdInscritas(t.Cod_Alumno, @Periodo, 0) AS UdInscritas
...
And p.Periodo < @Periodo
```

Therefore, in the current legacy configuration the SP receives `20262`, not an
empty string. An empty period only occurs when `SemestreDefecto` is missing or
blank in that deployment, or when the page is invoked outside its normal load.

### SADAlumnos verification

SADAlumnos does **not** currently read the default semester from AppConfig.
`ConfigurationInitialiser.PrepareAcademicConfigurationAsync` queries
`PeriodosAcademicos` for the period whose start/end dates contain today, and
publishes it through `AcademicSettings.DefaultSemester`. `/api/config/site`
exposes that value as `defaultSemester`. The AppConfig key `TuitionSemester`
is a separate tuition setting and is not the default academic period.

So the two references differ:

- Legacy SAD: `SemestreDefecto` AppConfig key.
- Modern SADAlumnos: active `PeriodosAcademicos` period.

For parity with the modern app, the target should use the SADAlumnos active
period source, not add a new AppConfig key or a period selector.

### Decision for the migration

Remove the **user-facing period selector**, but keep the period in the backend
request and SP call. Resolve it server-side from one of:

1. A SADInformes setting equivalent to `SemestreDefecto` (preferred for exact
   legacy parity), or
2. The active academic period, if the application already owns that concept.

Do not remove `@Periodo` from the SP contract. Sending an empty period changes
the report semantics: current enrolled UD becomes 0 and the previous-period
comparison stops being meaningful.

Show the resolved period in the page subtitle so the user knows which period
was used.

## Scope

- Remove the period `ng-select` and its filter wiring.
- Keep career filter.
- Add the legacy visual indicators and semantic colors.
- Remove the SADAlumnos student link.
- Match the legacy columns and place the photo inside the Nombre cell.
- Include Observaciones and Restricciones columns, even if the legacy leaves
  them empty.
- No schema or stored-procedure changes.

## Visual elements to add

### 1. Student photo and Nombre cell

"Photo with fallback" means: try `https://fotos.fen.uchile.cl/<Cod_Persona>.jpg`;
if the photo does not exist, show `https://fotos.fen.uchile.cl/perfilNN.jpg`.

Legacy layout: the photo is inside the **Nombre** cell, together with the
student name and `Situacion`, not in a separate photo column. Target must do
the same:

```text
[photo]  Nombre
         Situacion
```

Do not add a SADAlumnos link. Do not add a separate Foto column.
Optional PopBox-style zoom is a later enhancement; the fallback is required.

### 2. Previous permanence requests

Legacy shows:

```text
Periodo: [ok.gif if Aprobada] [no.gif if Rechazada] Autoridad
TipoDescriptivo
```

Current DTO exposes a formatted string, which loses the resolution state.
Change the contract to structured items:

```text
PreviousRequestDTO
  Period
  Type
  Resolution
  Authority
  Approved (bool)
```

Render:

- `bi-check-circle-fill` with `--success-green` for approved.
- `bi-x-circle-fill` with `--danger-red` for rejected/other non-approved.
- Period, authority and type as text.

### 3. Current permanence requests

Legacy shows period and type without a resolution icon. Keep as structured
items so the renderer does not need to parse text:

```text
CurrentRequestDTO
  Period
  Type
```

### 4. Elimination causes

Legacy renders each cause with `bolita_roja.gif` and text. Current DTO is one
newline string.

Change to:

```text
EliminationCauseDTO
  Text
  Severity (warning | danger)
```

Render a red/warning bullet (`bi-circle-fill`) using tokens:

- `--danger-red` for causes.
- `--warning-yellow` only if a cause is informational; otherwise keep danger.

### 5. Header/row colors

Do not copy the legacy hardcoded blue/gray palette. Use the project tokens:

- `--primary-blue`
- `--layout-text`
- `--layout-muted-text`
- `--success-green`
- `--danger-red`
- `--warning-yellow`
- `--layout-border-color`

## Table rendering approach

The current `app-table` supports multiline text, links, images and simple
badges, but not a list of icons per cell.

Preferred approach:

1. Extend the shared table with a small, generic `cellIcons` renderer:
   - `TableColumn.icons?: (row) => readonly CellIcon[]`
   - `CellIcon { icon: string; cssClass?: string; title?: string }`
2. Keep the existing multiline text columns for entry data, credit,
   performance, semesters and enrolled UD.
3. Use `icons` for:
   - previous request approved/rejected indicators;
   - elimination-cause bullets.
4. Keep the photo as the existing image column.

Alternative if the shared change is not desired: use a dedicated HTML table in
this report only. Prefer the shared extension to avoid duplicated table styles.

## Backend contract changes

Replace the formatted strings with structured arrays:

```text
UnresolvedPermanenceRowDTO
  ...
  PreviousRequests: PreviousRequestDTO[]
  CurrentRequests: CurrentRequestDTO[]
  EliminationCauses: EliminationCauseDTO[]
```

Keep the current grouping for:

- entry data;
- credit;
- performance;
- semesters;
- failed core courses;
- enrolled units.

The nested SP calls remain the same:

```text
spRec_AlumnoSolicitudes(..., 'PERMANENCIA RESUELTAS')
spRec_AlumnoSolicitudes(..., 'PERMANENCIA PENDIENTES')
spRec_AlumnoReprobadasCore(...)
```

The existing per-student N+1 behavior remains a documented performance
follow-up; this plan does not change it.

## Filter changes

Remove:

- `period` from the Angular form;
- period selector from the template;
- period option loading from `/filters`.

Keep:

- career selector.

Backend report request should become:

```text
GET /report?career=IC
```

The server resolves the period from the SADAlumnos active-period source and
still passes it to the SP. Show that resolved period in the page subtitle.

## Tests

- Period is resolved server-side and passed to the SP.
- No period selector is rendered.
- Career filter still reloads the report.
- Approved previous request renders green check.
- Rejected previous request renders red cross.
- Each elimination cause renders one bullet.
- Name cell renders photo + name + situation.
- Photo fallback works.
- No SADAlumnos link.
- Error/empty states remain sanitized.

## Phases

### T1 — Period decision and filter removal

- Choose `SemestreDefecto`-equivalent setting vs active period.
- Remove period selector and wire server-side default.
- Update filters/report contracts and tests.

### T2 — Structured DTOs

- Add previous/current request and elimination-cause DTOs.
- Map nested SP rows without losing resolution state.
- Update focused backend tests.

### T3 — Shared icon renderer

- Add generic `cellIcons` support to `app-table`.
- Use it for previous requests and elimination causes.
- Keep colors token-based.

### T4 — Visual verification and docs

- Photo fallback and zoom decision.
- Update `docs/requests/unresolved-permanence.md`.
- Run focused and full suites; no commit without explicit approval.
