# NCE notifications migration
Migrate the NCE report from Maestro into SADInformes as read-only, after an explicit scope change to remove all management. Delivery: `feat/postgraduate-nce`; no commits until explicit user approval.

## Specs
S1. "Informes/Postgrado/InformeNce.aspx , este informe esta en Maestro verifica"
S2. "hay que migrar todo lo de maestro del informe"
S3. "no hagas un commit espera que yo te de el visto bueno"

## Decisions
- Do not replicate the SAD iframe or the Base64 `AdminPermiso` cookie.
- Implement native SADInformes pages backed by the SAD tables used by Maestro:
  `NotificacionCausalEliminacionPostgrado`, `CausalEliminacionPostgrado`,
  `TipoCausalEliminacionPostgrado`.
- The user later requested removing all management and keeping only the report.
- Only the **Informe** view is migrated: 9 data columns plus row number, search,
  sort and exports Excel/CSV. PDF is offered through the browser print dialog.
- No notification listing, email modal, block/activate, write endpoint, manage
  permission or `OmitirProximaNotificacion` mutation.
- Single permission: `REPORT_VIEW_POSTGRADUATE_NCE`.
- No iframe or `AdminPermiso` cookie.
- No schema changes and no commits.

## Tasks
T1 [S1,S2] DONE: report-only SAD queries, permission, report/download endpoints; management removed. 3 focused tests GREEN.
T2 [S2] DONE: Angular report-only route with shared table/search and Excel/CSV/print exports; 34 focused UI tests GREEN.
T3 [S1,S2,S3] DONE: documentation, full suites/build and final report after the report-only scope change; .NET 525, Node 38, Angular 301/306, build GREEN.

## Log
L1. Informes/Postgrado/InformeNce.aspx , este informe esta en Maestro verifica
L2. Confirmed SADInformes only had a pending catalogue entry; legacy SAD page was an iframe/cookie wrapper. Maestro has NCEPostgrado Index/Informe plus Bloquear/Activar.
L3. hay que migrar todo lo de maestro del informe
L4. Created feat/postgraduate-nce from clean master. Scope includes both Maestro views and the write actions, migrated natively.
L5. Backend and UI complete. Focused .NET 3/3 and UI 35/35 GREEN. Documentation added.
L6. Architecture test rejected the command name SetNceOmit; renamed to UpdateNceOmit. Architecture 4/4, NCE 9/9, full .NET 525/525 and Node 38/38 GREEN. Angular 302/307 with the five known Node 26 localStorage failures; Vite build GREEN. No commits or staging.
L7. User requested removing all management and keeping only the report. Removed notification listing, email modal, block/activate, write endpoint and manage permission. Focused NCE 9/9 and UI 34/34 GREEN.
L8. Full .NET 525/525 and Node 38/38 GREEN; Angular 301/306 with the five known Node 26 localStorage failures; Vite build GREEN. `git diff --check` clean after normalizing CR in two edited files. No commits or staging.
L9. Added shared export color tokens/classes: Excel green `--export-excel` and PDF red `--export-pdf`. NCE Excel uses `btn-export-excel`; PDF now says PDF, uses `bi-file-earmark-pdf` and `btn-export-pdf`, no Imprimir label. Focused UI 34/34 and build GREEN. No commit.
L10. PDF moved to backend: PDFsharp/MigraDoc 6.2.2, Carlito fonts embedded, Legal landscape, endpoint `/download/pdf`, UI downloads the PDF without pdfmake. Focused NCE 10/10 and UI tests updated.
L11. Full .NET 526/526 and Node 38/38 GREEN; Angular 301/306 with the five known Node 26 localStorage failures; Vite build GREEN. `git diff --check` clean. No commits or staging.
