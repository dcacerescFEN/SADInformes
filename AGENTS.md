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
- Module-level interfaces such as `ITeacherSurveyReader` stay at the module root;
  shared validation without a module owner goes in `Application/Common`.

### DTOs

- DTOs are `public sealed record` types.
- A DTO belongs to the action that uses it: same folder, its own file, named
  `<Name>DTO.cs` (for example `Queries/GetCourse/CourseDTO.cs`).
- Query request/response records, filters and row shapes are DTOs too. Do not put
  action-specific DTOs in `Common`; `Common` only holds genuinely shared contracts.

### Database contexts and queries

- `SadContext` is the database definition: `DbSet` properties and their table,
  key, column, length, Unicode and collation mappings. It contains no custom query,
  command, SQL execution or transaction helpers.
- New table mappings are taken from the canonical models in `MappingDB/Models`.
  Add only the tables and columns the feature uses; never reference the whole
  reverse-engineered project or change the database.
- Queries are LINQ over mapped `DbSet`s in the data adapter that owns the feature
  (for example `MassEnrollmentData.FindCourseAsync`). Do not issue ad-hoc
  `SELECT` strings from Application, endpoints or data adapters.
- Existing stored procedures stay in `SadContextProcedures` with typed,
  parameterized methods. Build their parameters there and execute through the
  context connection; never concatenate values into SQL.
- Transactions, command execution and business postconditions belong to the data
  adapter that owns the operation, not to `SadContext`.
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
