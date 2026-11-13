# Refactor: correo dentro del feature Alumnos sin fotos

Estado: implementado. Sin cambios de contrato HTTP.

## Decisión

El correo pertenece a **Alumnos sin fotos**, no a un módulo aparte.

Se adopta una sola feature:

```text
StudentsWithoutPhoto/
  Commands/
  Queries/
  Common/
```

y un solo endpoint:

```text
StudentsWithoutPhotoEndpoints
```

Se elimina `StudentsWithoutPhoto/Email/` como submódulo y
`StudentsWithoutPhotoEmailEndpoints` como endpoint separado.

Motivo: `AGENTS.md` exige `Commands` y `Queries` en la raíz de cada módulo. Tener
la lista en `Queries/` y el correo en `Email/Commands` rompe esa estructura y
duplica grupo, permiso y tag.

## Estructura objetivo

```text
src/Application/Reports/AcademicEnrollment/StudentsWithoutPhoto/
  Commands/
    CancelStudentsWithoutPhotoEmail/
      CancelStudentsWithoutPhotoEmail.cs
    StartStudentsWithoutPhotoEmail/
      StartStudentsWithoutPhotoEmail.cs
      EmailDispatchStartedDTO.cs
  Queries/
    GetStudentsWithoutPhoto/
      GetStudentsWithoutPhoto.cs
      StudentWithoutPhotoRowDTO.cs
      StudentsWithoutPhotoReportDTO.cs
    GetStudentsWithoutPhotoPage/
      GetStudentsWithoutPhotoPage.cs
      StudentWithoutPhotoPageRowDTO.cs
      StudentsWithoutPhotoPageDTO.cs
    GetStudentsWithoutPhotoEmailAvailability/
      GetStudentsWithoutPhotoEmailAvailability.cs
      EmailAvailabilityDTO.cs
    GetStudentsWithoutPhotoEmailPreview/
      GetStudentsWithoutPhotoEmailPreview.cs
      EmailPreviewDTO.cs
    GetStudentsWithoutPhotoEmailStatus/
      GetStudentsWithoutPhotoEmailStatus.cs
  Common/
    Email/
      EmailDispatchConflictException.cs
      EmailDispatchJob.cs
      EmailDispatchQueue.cs
      EmailDispatchRegistry.cs
      EmailDispatchSnapshot.cs
      EmailDispatchStatus.cs
      StudentsWithoutPhotoEmailRecipients.cs
      StudentsWithoutPhotoEmailTemplate.cs
```

Reglas:

- Los comandos y consultas de correo dejan de vivir bajo `Email/`.
- `Common/Email` solo contiene contratos compartidos por más de una acción:
  registry, cola, job, snapshot, estado, conflicto, plantilla y destinatarios.
- `EmailDispatchSnapshot` se mantiene como contrato compartido entre estado y
  cancelación; no se duplica por acción.
- Los DTO de una acción permanecen en la carpeta de esa acción.
- `EmailAvailabilityDTO` y `EmailPreviewDTO` pasan a sus queries respectivas.
- `GetStudentsWithoutPhotoEmailAvailability` y
  `GetStudentsWithoutPhotoEmailPreview` reemplazan la lógica directa del endpoint.
- No cambian rutas, payloads, permisos ni comportamiento.

## Endpoint único

`StudentsWithoutPhotoEndpoints` conserva un solo grupo:

```text
/api/reports/academic-enrollment/students-without-photo
```

Rutas:

```text
GET  /report
GET  /export
GET  /email/availability
GET  /email/preview
POST /email
GET  /email/{jobId}
POST /email/{jobId}/cancel
```

Un solo `RequireAuthorization(Permission.REPORT_ACCESS)` y un solo
`WithTags("Reports/AcademicEnrollment")`.

Helpers compartidos del endpoint:

- `Problem(status, title)`
- `Invalid(message)`
- `Unavailable()`
- `IsUnavailable(error)`

Se elimina `StudentsWithoutPhotoEmailEndpoints.cs`.

## Cambios de namespace

Antes:

```text
Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Email.Commands...
Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Email.Queries...
Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Email.Common
```

Después:

```text
Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Commands...
Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Queries...
Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email
```

Actualizar:

- `Infrastructure/ConfigureServices.cs`
- `Infrastructure/Services/EmailDispatchWorker.cs`
- `WebApi/Endpoints/Reports/AcademicEnrollment/StudentsWithoutPhotoEndpoints.cs`
- `tests/WebApi.Tests/StudentsWithoutPhotoEmailTests.cs`
- `tests/WebApi.Tests/EmailSettingsConfigurationTests.cs` si referencia namespaces
- imports del frontend no cambian

## Pasos (completados)

1. Crear `Common/Email` y mover los contratos compartidos.
2. Mover comandos de correo a `Commands/`.
3. Mover consultas de correo a `Queries/`.
4. Crear handlers de availability y preview.
5. Fusionar los endpoints de correo en `StudentsWithoutPhotoEndpoints`.
6. Eliminar la carpeta `Email/` y el endpoint separado.
7. Actualizar namespaces e imports.
8. Ejecutar pruebas y documentación.

## Verificación

- `WebApi` completo verde.
- `Angular` completo verde.
- `Node` completo verde.
- Build Angular correcto.
- Pruebas de correo: preview, availability, start 202, status, cancel, disabled,
  fallo parcial y configuración AppConfig.
- Verificar por `rg` que no queda
  `StudentsWithoutPhoto.Email.Commands`, `.Email.Queries` ni
  `StudentsWithoutPhotoEmailEndpoints`.
- Verificar que las rutas HTTP siguen siendo idénticas.

## Riesgos

- Cambio mecánico de namespaces; el compilador detecta referencias faltantes.
- El endpoint único queda más grande; se mantiene legible con helpers y métodos
  privados por acción.
- No se modifica la lógica de envío, cancelación, worker ni frontend.

## Fuera de alcance

- Cambiar la UI.
- Cambiar el contrato HTTP.
- Cambiar la configuración SMTP.
- Introducir tablas o persistencia.
