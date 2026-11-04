# Basic right annual payment migration
Migrate `PagoDerechoBasicoAnual.aspx` to `/postgrado/pago-derecho-basico-anual`, including payment listing, amount by year, payment selection and transactional save. Delivery: `feat/postgraduate-basic-right-payment`; no commits until explicit user approval.

## Specs
S1. "ahora spRec_AlumnosXPago_Derecho_Basico"
S2. "ahora Informes/Postgrado/PagoDerechoBasicoAnual.aspx"
S3. "no hagas un commit espera que yo te de el visto bueno"

## Decisions
- Preserve filters: student type, modality, entry year, payment year and payment status.
- Preserve the grid columns: Posición, photo, Nombre, Rut, Código Alumno, Semestre de Ingreso, Situación, Promedio and Pago.
- Amount by payment year is read from `PagoDerechoBasicoAnual_Montos`; when absent, saving is blocked with "Sin Monto".
- Save only rows whose `Pago` changed, using the authenticated server identity, not request JSON.
- Save runs in one Serializable transaction through `spCre_AlumnosXPago_Derecho_Basico`.
- Use `spRec_AlumnosXPago_Derecho_Basico` for the listing and `spRec_TiposAlumnosModalidades` for dependent modalities.
- Keep the legacy Excel export columns (without photo/payment), as XLSX.
- Separate permissions: `REPORT_VIEW_POSTGRADUATE_BASIC_RIGHT_PAYMENT` and `REPORT_MANAGE_POSTGRADUATE_BASIC_RIGHT_PAYMENT`.
- Do not modify SPs or schema.

## Tasks
T1 [S1,S2] DONE: typed SPs, amount mapping, filters, listing, save command, permissions and endpoints; 3 focused tests GREEN.
T2 [S2] DONE: Angular route/page with filters, amount, shared table, payment selection and save confirmation; 35 focused UI tests GREEN.
T3 [S1,S2,S3] DONE: documentation, full suites/build and final report; .NET 531, Node 38, Angular 306/311, build GREEN.

## Log
L1. ahora spRec_AlumnosXPago_Derecho_Basico
L2. ahora Informes/Postgrado/PagoDerechoBasicoAnual.aspx
L3. Started from clean master; created feat/postgraduate-basic-right-payment. Legacy: five filters, amount by payment year, ranking grid with photo and payment checkboxes, save through spCre, export filename Ranking_Programas_Graduados.xls.
L4. Backend and UI complete. Focused .NET 3/3 and UI 35/35 GREEN. Documentation added.
L5. Full .NET 529/529 and Node 38/38 GREEN; Angular 306/311 with the five known Node 26 localStorage failures; Vite build GREEN. `git diff --check` clean. No commits or staging.
L6. Photos were invisible because the URL was hardcoded. Added StudentPhotoSettings from AppConfig `StudentPhotoBaseUrl` (same key and validation as SADAlumnos), injected into the payment query, and tested normalization/default. Focused 12/12 GREEN.
L7. Full .NET 531/531 and Node 38/38 GREEN; Angular 306/311 with the five known Node 26 localStorage failures; Vite build GREEN. `git diff --check` clean. No commits or staging.

L8. Photo identifier corrected: SADAlumnos names photos with `Cod_Persona`, not Rut. The report now resolves CodPersona by CodAlumno and builds `{StudentPhotoBaseUrl}{CodPersona}.jpg`; fallback `perfilNN.jpg`. Focused 3/3, full .NET 531/531, Node 38/38, diff clean. No commit.
L9. Photo column was blank because the component used `photo` while the API/DTO exposes `photoUrl`. Aligned the row interface and table column key to `photoUrl`, updated the fixture and added an `<img src>` assertion. Focused 4/4, full Angular 306/311 (five known Node 26 localStorage failures), Vite build GREEN. No commit.
L10. Filter row rebalanced: Tipo Alumno col-xl-4, Año Ingreso and Año del Pago col-xl-1, Modalidad/Situación/Mostrar col-xl-2. Focused UI 35/35 and Vite build GREEN. No commit.
L11. Added reusable `TableColumn.link` support and linked Código Alumno to SADAlumnos using `studentsMenuUrl` (`modo=4`, `codigo`), target `_blank` and `noopener noreferrer`. Focused 42/42, full Angular 307/312 (five known Node 26 localStorage failures), Vite build GREEN. No commit.
L12. Corrected the SADAlumnos link: existing reports use `/students/detail/{Cod_Alumno}`, not the legacy `alumnos/default.aspx?modo=4`. Aligned Basic Right Payment and the shared-table link test. Focused 42/42 and Vite build GREEN. No commit.