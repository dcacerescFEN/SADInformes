# Solicitudes — confirm print and validation comment plan

Plan for the two remaining actions of `SolicitudesXEstado`:

1. **Confirmar impresión** (`btnConfirmar`).
2. **Editar comentario de validación** (`txtComentarioValidacion`).

Crystal Reports detail printing is explicitly out of scope; it will be handled
separately after checking whether an SSRS equivalent exists.

## Current state

Read-only reports already exist:

```text
/requests/automatic-processing-requests
/requests/non-printed-postgraduate-course-recognition   (type 67)
/requests/non-printed-seminar-homologation-magister     (type 65)
/requests/non-printed-other-types                       (OTROS)
/requests/pending-requests                              (PENDIENTE)
/requests/unresolved-permanence
```

`SolicitudesXEstado` already exposes:

- `ComentarioValidacion` as read-only text.
- No print/confirm actions.
- No comment edit action.
- Only `REPORT_VIEW_REQUESTS`.

## Legacy behavior to preserve

### Confirmar impresión

Legacy `btnConfirmar_Click`:

1. Shows the browser confirmation `Confirma la Impresión`.
2. Iterates every visible grid row.
3. Calls `spMod_Solicitud_Impresa` for each `Cod_Solicitud`.
4. Rebinds the grid; rows marked `IMPRESA` disappear from the
   `NO IMPRESA` result.

SP:

```sql
UPDATE Solicitudes89
SET Estado_Impresion = 'IMPRESA'
WHERE Cod_Solicitud = @Cod_Solicitud
```

Legacy visibility: the button is hidden only when the grid has no rows; it is not
restricted to `NO IMPRESA`. For the migration, show it only on the
`NO IMPRESA` reports to avoid marking pending requests as printed by accident.
If exact legacy parity is required, document that decision explicitly.

### Editar comentario

Legacy:

- Textarea per row, `MaxLength = 400`.
- Visible/editable only with
  `editar comentario validacion solicitudes`.
- Saves on `TextChanged` with:

```sql
UPDATE Solicitudes89
SET ComentarioValidacion = @Comentario
WHERE Cod_Solicitud = @CodSolicitud
```

- Empty text is stored as `NULL`.

## Permissions

Add to `src/Domain/Constants/Permission.cs`:

```text
REPORT_PRINT_REQUESTS
REPORT_EDIT_REQUEST_VALIDATION
```

Legacy equivalences:

```text
imprimir solicitudes
  > REPORT_VIEW_REQUESTS
    REPORT_PRINT_REQUESTS

editar comentario validacion solicitudes
  > REPORT_VIEW_REQUESTS
    REPORT_EDIT_REQUEST_VALIDATION
```

Update:

- `Permisos/Equivalencias-Permisos.md`
- `Permisos/Descripciones-Permisos.md`
- Permission tests/docs.

Backend endpoints:

- Confirm print requires `REPORT_PRINT_REQUESTS`.
- Comment edit requires `REPORT_EDIT_REQUEST_VALIDATION`.
- Both still require the existing module access and view permission.

## Backend: confirm print

Add a typed SP wrapper:

```text
ISadContextProcedures.SpMod_Solicitud_ImpresaAsync(string? codSolicitud)
```

Implemented with `ExecuteSqlRawAsync`, matching the legacy SP signature.

Command:

```text
ConfirmRequestsPrinted(IReadOnlyList<string> RequestCodes)
```

Validation:

- non-empty list;
- distinct, non-empty codes;
- maximum batch size (for example 1,000) to bound the request;
- reject malformed/overlong codes.

Behavior:

- Start a transaction with `ISadContext.BeginTransactionAsync`.
- Call `spMod_Solicitud_Impresa` for each code.
- Commit only after all calls succeed.
- Return the number of processed requests.

Endpoint:

```text
POST /api/reports/requests/by-status/confirm-print
```

Body:

```json
{ "requestCodes": ["2024200001", "2024200002"] }
```

Return `200` with the processed count or `204`; prefer `200` so the UI can show
the count.

The UI sends the **codes currently visible in the result**, not a fresh query, so
the action marks exactly what the user saw. Do not mark all rows by filter alone
without re-reading; a stale list must not mark unseen rows.

## Backend: edit validation comment

The existing minimal `Solicitud89` mapping already contains
`ComentarioValidacion`. Add a controlled write path.

Option A (preferred): expose a safe persistence method in `ISadContext`:

```text
Task<int> SaveChangesAsync(CancellationToken cancellationToken)
```

and implement it by delegating to `SadContext.SaveChangesAsync`.

Command:

```text
UpdateRequestValidationComment(string RequestCode, string? Comment)
```

Validation:

- `RequestCode` required, maximum 20 characters;
- comment trimmed;
- comment maximum 400 characters, matching the legacy textarea;
- empty/whitespace becomes `NULL`.

Behavior:

- load the request by `CodSolicitud`;
- return not found when absent;
- update only `ComentarioValidacion`;
- save with cancellation.

Endpoint:

```text
PUT /api/reports/requests/by-status/comment
```

Body:

```json
{ "requestCode": "2024200001", "comment": "Texto de validación" }
```

Return `204` on success; `400` for validation, `404` for an unknown request,
`503` for storage failures.

## Frontend

### Confirm print

- Add a button in the result header, visible only:
  - on the `NO IMPRESA` reports;
  - with `REPORT_PRINT_REQUESTS`;
  - when there are rows.
- Icon: `bi-check2-square` or `bi-printer-fill`.
- Use `NotificationService.confirm` with the legacy wording.
- Disable while submitting.
- On success:
  - toast `Impresión confirmada`;
  - reload the current result so marked rows disappear.
- On error: sanitized alert, no optimistic row removal.

### Edit comment — inline edit like Disponibilidad Horaria

Use the same interaction pattern already implemented in the availability editor.

- Add an `Acciones` column with an icon-only `bi-pencil` action, visible with
  `REPORT_EDIT_REQUEST_VALIDATION`.
- Tooltip/aria-label: `Editar comentario de validación`.
- On click:
  - mark that row as editing (only one row at a time);
  - replace the `Comentario Validación` text with a `textarea`
    (`maxlength="400"`, autofocus);
  - change the action button to `bi-save` with tooltip
    `Guardar comentario`.
- On save:
  - send `PUT /api/reports/requests/by-status/comment`;
  - on success, replace the textarea with the saved text in memory, return the
    action to `bi-pencil`, and show the toast `Comentario guardado`;
  - on error, keep the textarea and editing state so the user can retry.
- Empty comment is allowed and clears the value.
- No modal. Do not save on blur or on every keystroke.
- `Confirmar impresión` never saves the comment.

### Table support for inline editing

The availability editor is a **dedicated native HTML table** with its own
`@if (editing)` and `ng-select`; that is why it did not need an `app-table`
change.

`RequestsByStatus` currently uses the shared `app-table`, which renders text,
links, avatars and icon lists but has no inline editor. Extending `app-table` is
therefore a reuse decision, not a requirement. The alternatives are:

1. Extend `app-table` with a small generic editable-text contract (preferred
   for reuse; keeps search, sorting, pagination and skeleton behavior).
2. Use a dedicated native table for this report only, exactly like the
   availability editor. This avoids touching the shared table, but duplicates
   the report table behavior.

If the shared component must remain untouched, choose option 2 and port only the
required search/sort behavior for this screen.

The proposed shared contract is:

```text
TableColumn.editText?: {
  editing: (row: object) => boolean;
  value: (row: object) => string;
  maxLength?: number;
  save: (row: object, value: string) => void;
}
```

Render a textarea in the generic text branch when `editing(row)` is true and
emit the save callback from the table component. Keep the table action column
for the pencil/save icon. This avoids a report-specific HTML table and keeps the
shared table conventions.

Alternative only if the shared change is rejected: a dedicated table for this
report. Prefer the shared extension.

## Tests

### Backend

- Confirm print:
  - calls `spMod_Solicitud_Impresa` once per code;
  - rejects empty/duplicate/oversized input;
  - commits transaction on success and rolls back on failure;
  - permission boundary.
- Comment:
  - trims and limits to 400;
  - empty becomes null;
  - updates only the selected request;
  - unknown code returns not found;
  - permission boundary.

### Angular

- Confirm button hidden without permission and on pending/other reports.
- Confirmation calls the endpoint with current visible codes.
- Success reloads and removes printed rows.
- Comment action hidden without permission.
- Edit switches the comment cell to a textarea and the action to save.
- Save updates the row in memory and returns to text.
- Only one row edits at a time; errors keep the editor open.
- Error paths show sanitized messages.

## Out of scope

- Crystal `ImprimeSolicitud.aspx` and `spRec_SolicitudInscripcion`.
- Printing detail by `desde`/`hasta` range.
- Any SSRS replacement; the user will verify the Crystal/SSRS path separately.
- Schema or stored-procedure changes.
- Changes to unresolved-permanence.

## Phases

### T1 — Permissions

- Add the two permissions, equivalences and descriptions.
- Add permission-boundary tests.

### T2 — Confirm print — DONE

- Added `REPORT_PRINT_REQUESTS`, SP wrapper, command, endpoint and transaction tests.
- Added UI button, confirmation, toast and reload.

### T3 — Comment edit — DONE

- Added `REPORT_EDIT_REQUEST_VALIDATION`, persistence method, command and endpoint.
- Added inline `app-table` edit contract, action column and tests.

### T4 — Documentation and verification

- Update `docs/requests/requests-by-status.md`.
- Run focused and full suites/build.
- No commit without explicit approval.
