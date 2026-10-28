# SADInformes conventions

These definitions apply to this repository. They describe how backend and frontend
code is organized; they do not replace repository security, permission or review rules.

## Backend

### Layers

- Dependencies point inward: Domain <- Application <- Infrastructure; WebApi references Application and Infrastructure.
- Application contains use cases and contracts, not database implementation.
- Infrastructure implements persistence and external adapters.

### Application modules

Every module under `src/Application` (for example `Config`, `Reports/Postgraduate/<Report>`) exposes
its use cases through `Commands` and `Queries` folders:

```text
<Module>/
  Commands/
    <Action><Name>/
      <Action><Name>.cs
      <Name>DTO.cs
      ...
  Queries/
    Get<Name>/
      Get<Name>.cs
      <Name>DTO.cs
      ...
```

- Commands start with `Create`, `Update`, `Delete`, `Upload` or `Download`.
  Example: `Commands/CreateCourse/CreateCourse.cs`.
- Queries always start with `Get`. Example: `Queries/GetCourse/GetCourse.cs`.
- One request and its handler per action. Never group several actions in a shared
  `*Queries.cs` or `*Commands.cs` file.
- Endpoints dispatch requests with `IRequestDispatcher`; handlers return
  `ApiResponse<T>` through `IApiResponseService`.
- Shared module queries stay within their owning Application module; do not call
  one handler from another. Shared validation without an owner goes in `Application/Common`.

### DTOs

- DTOs are `public sealed record` types.
- A DTO belongs to the action that uses it: same folder, its own file, named
  `<Name>DTO.cs` (for example `Queries/GetCourse/CourseDTO.cs`).
- Query request/response records, filters and row shapes are DTOs too. Do not put
  action-specific DTOs in `Common`; `Common` only holds genuinely shared contracts.

### Database contexts and queries

- `ISadContext` in Application exposes mapped tables as read-only `DbSet<T>` getters
  and generic transaction creation. `SadContext` in Infrastructure implements it,
  owns mappings and starts transactions on the same connection used by the SP wrappers.
  The returned `IDbContextTransaction` supports commit, rollback and async disposal;
  disposal of an uncommitted transaction rolls it back. No module-specific queries
  or business rules belong in `SadContext`.
- New table mappings are taken from the canonical models in `MappingDB/Models`.
  Add only the tables and columns the feature uses; never reference the whole
  reverse-engineered project or change the database.
- Queries are LINQ over `ISadContext` in each Application module. Handlers consume
  `ISadContextProcedures` directly for typed SP calls. Application may depend on EF Core,
  but never on Infrastructure, concrete contexts, SqlClient or ad-hoc SQL.
- Stored procedures stay in `SadContextProcedures`, implementing the Application
  contract `ISadContextProcedures`. Follow the canonical `MappingDB/Models` wrappers:
  typed nullable parameters, sizes, `DBNull`, optional `OutputParameter<int>` and cancellation.
  Read SPs use `SqlQueryAsync<T>`; non-query SPs such as administrative enrollment use
  `ExecuteSqlRawAsync`. Never concatenate values or confuse affected rows with SP return values.
- Application commands define transaction boundaries, isolation and business postconditions.
  Generic setup belongs in `SadContext.BeginTransactionAsync` (including `XACT_ABORT ON`).
  Preserve Serializable isolation and same-context SP participation for enrollment batches.
- Audit identity and client address come from the authenticated server context,
  never from request JSON.

### Endpoints and errors

- Declare authorization explicitly and keep report permissions specific.
- Return the shared response envelope; reject invalid input before reading data.
- Storage failures return a sanitized 503 without SQL details or secrets.

## Frontend

- Angular 22 standalone components with lazy routes loaded from the postgraduate
  catalogue; preserve existing routes and slugs.
- Use `ChangeDetectionStrategy.OnPush` and signals for report state.
- Reuse the shared `app-table`, `ng-select` floating-label filters,
  `page-header`/`page-title`/`page-subtitle` layout and the SADAlumnos-style
  `NotificationService` (SweetAlert2) for confirmation and notifications.
- Every destructive or academic write requires explicit confirmation and must not
  resend a stale selection after the dialog resolves.
- Disable actions from authoritative server flags (for example `canEnroll`), not
  from inferred client state alone.
- CSV exports use UTF-8 BOM, semicolon separators and safe escaping; never allow
  spreadsheet formula injection.
- Consume `{ success, message, data }`; show sanitized errors; never print SQL
  details. Keep tokens in memory and never persist secrets.
- Filter changes discard stale candidates and selections; older HTTP responses
  must not overwrite newer UI state.

## Verification

From `SADInformes`:

```sh
dotnet build SADInformes.slnx
dotnet test tests/WebApi.Tests/WebApi.Tests.csproj
node --test tests/*.test.mjs
cd src/Web/WebUI && pnpm test && pnpm build
```

If a running API locks default DLL paths, use `--artifacts-path .artifacts/<name>`
instead of stopping the user's process.
