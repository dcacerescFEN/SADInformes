# Plan: migración del correo masivo de Alumnos sin fotos

Estado: implementado. No crea tablas ni campañas persistentes.

## 0. Implementación actual

- `Infrastructure/Configuration/EmailSettings.cs` y `EmailDeliverySettings`.
- `Infrastructure/Services/SmtpEmailSender.cs` y `EmailDispatchWorker.cs`.
- `Application/.../StudentsWithoutPhoto/Commands/**` y `Queries/**`: comandos,
  consultas, disponibilidad y preview.
- `Application/.../StudentsWithoutPhoto/Common/Email/**`: plantilla, destinatarios,
  cola y registry.
- `WebApi/Endpoints/.../StudentsWithoutPhotoEndpoints.cs` (grupo único del informe).
- `core/tasks/background-task-center.ts` y `background-task-host.ts`.
- `students-without-photo/email-dispatch-service.ts`.

## 1. Decisiones cerradas

- Se conserva el permiso actual del informe: `REPORT_ACCESS_REPORT_MODULE`.
  No se agrega un permiso de envío.
- `EmailSettings` sigue el formato de **SADAlumnos**, no las claves legacy de
  `web.config`.
- Del legacy se conserva únicamente el correo: destinatarios, remitente, asunto,
  BCC y el texto de `Button1_Click`.
- La plantilla HTML se mantiene fija y aprobada.
- No hay tabla de campañas.
- El envío es asíncrono: la request no espera a que se emitan todos los correos.
- No hay historial persistente ni reintento después de reiniciar el proceso.

## 2. Configuración: formato SADAlumnos

### Infrastructure

`Infrastructure.Configuration.EmailSettings`, con el mismo formato estático de
SADAlumnos:

```csharp
public static class EmailSettings
{
    public static bool SendEmail { get; }
    public static string EmailFrom { get; }
    public static string EmailTo { get; }
    public static string Server { get; }
    public static int Port { get; }

    public static void Initialize(bool sendEmail, string emailFrom, string emailTo,
        string server, int port);
}
```

Claves de AppConfig:

| Clave | Default SADAlumnos |
|---|---|
| `SendEmail` | `false` |
| `EmailFrom` | `secretariaestudios@fen.uchile.cl` |
| `EmailTo` | `dcaceresc@fen.uchile.cl` |
| `EmailServer` | `smtp-mailhog-dev.fen.uchile.cl` |
| `EmailPort` | `1025` |

`ConfigurationInitialiser`:

- Lee esas claves desde `Configurations`.
- Valida `EmailFrom` y `EmailTo` como direcciones.
- Valida `EmailServer` como host.
- Valida `EmailPort` entre 1 y 65535.
- Inicializa `EmailSettings` y `EmailDeliverySettings`, igual que SADAlumnos.

No se usan `EnvioDeCorreo:*`, `Ambiente`, `correoPrueba`, `OnlySaveEmail` ni
`SaveEmail`. Esas claves pertenecen al formato legacy y no se migran.

### Application

`EmailDeliverySettings` con el mismo patrón de SADAlumnos:

```csharp
public static bool IsEnabled { get; }
public static bool IsSmtpConfigured { get; }
public static string Recipient { get; }
public static string Sender { get; }
```

### Envío

Se reutiliza el contrato de SADAlumnos:

```csharp
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
```

Con `SmtpEmailSender` equivalente:

- `SmtpClient(EmailSettings.Server, EmailSettings.Port)`.
- `From = EmailSettings.EmailFrom`.
- Sin credenciales explícitas, igual que SADAlumnos.
- `EmailMessage` con `To`, `Cc`, `Bcc`, `Subject`, `Body` y adjuntos vacíos.

`SendEmail = false` o SMTP incompleto: no se envía y se devuelve un error de
configuración sanitizado.

## 3. Correo legacy conservado

### Remitente, asunto y BCC

```text
From: secretariaestudios@fen.uchile.cl
Subject: Fotografía Web de Docencia
Bcc: secretariaestudios@fen.uchile.cl
```

### Destinatarios

- Si `MailFen` existe:
  - `To = MailFen`
  - `Cc = E_Mail` si existe y es válido
- Si `MailFen` no existe:
  - `To = E_Mail`
- Si ninguno existe o es inválido:
  - Se omite y se cuenta.

Deduplicar por `Cod_Alumno`.

### Plantilla HTML

Se conserva el texto exacto de `Button1_Click`:

```html
<h2>Estimados Estudiantes,</h2>

<p>
  Informamos a ustedes que a la brevedad deberán pasar por los módulos de
  Secretaría de Estudios, entre las 9.30 hrs y las 17.30 hrs., a tomarse la
  fotografía para ser incorporada al Web de Docencia. De no estar registrada su
  fotografía al 13 de junio, serán suspendidos todos los servicios otorgados por
  Secretaría de Estudios.
</p>

<p><strong>SECRETARIA DE ESTUDIOS</strong></p>
```

- `IsBodyHtml = true`.
- No hay edición de HTML ni texto libre desde el navegador.
- La fecha textual `13 de junio` se conserva en esta primera versión.

## 4. Envío asíncrono sin tabla de campañas

No se persiste nada en base de datos. El estado vive en memoria del proceso.

### Componentes

- `EmailDispatchQueue`: cola en memoria (`Channel<Guid>` o equivalente).
- `EmailDispatchRegistry`: diccionario en memoria con estado y contadores.
- `EmailDispatchWorker`: `BackgroundService` que consume la cola y envía.
- `IEmailSender`: envío SMTP por destinatario.
- Bloqueo por usuario/proceso para evitar doble clic.

### Flujo

```text
POST /email
  → valida permiso y configuración
  → obtiene alumnos sin foto
  → crea jobId en memoria
  → encola el job
  → responde 202 Accepted

BackgroundService
  → envía secuencialmente o en lotes pequeños
  → actualiza contadores en memoria

GET /email/{jobId}
  → estado y contadores
```

Respuesta inicial:

```json
{ "jobId": "...", "total": 120, "state": "QUEUED" }
```

Estados:

```text
QUEUED | RUNNING | COMPLETED | PARTIAL | FAILED
```

Contadores:

```text
enviados | omitidos | fallidos
```

Limitaciones explícitas:

- El job se pierde si se reinicia el proceso.
- No hay historial ni reintento después de reiniciar.
- En múltiples instancias, el `jobId` debe resolverse en la misma instancia. Para
  esta migración se asume una instancia o afinidad de sesión.
- Si el proceso se cae a mitad del envío, los correos ya emitidos no se pueden
  saber desde la aplicación.

## 5. Opciones de UX: bloquear o no la página

### Opción A — Bloquear el modal hasta terminar

- El usuario confirma y espera con el modal abierto.
- El frontend consulta el estado hasta `COMPLETED/PARTIAL/FAILED`.
- No puede navegar ni ejecutar otra acción en esa pantalla.
- Ventaja: resultado inmediato y simple.
- Desventaja: no cumple del todo el objetivo de liberar al usuario; si cierra la
  pestaña, el envío continúa pero no ve el resultado.
- No recomendada.

### Opción B — Modal con progreso, sin bloquear la navegación

- La request responde `202` y el modal muestra “Enviando en segundo plano”.
- El usuario puede cerrar el modal y seguir trabajando.
- El job continúa en el `BackgroundService`.
- Si vuelve a la pantalla, consulta el último `jobId` guardado en
  `sessionStorage` y muestra el estado.
- Ventaja: no bloquea y mantiene visibilidad.
- Desventaja: el estado es por pestaña/proceso y se pierde al reiniciar.
- **Recomendada.**

### Opción C — Envío completamente en segundo plano, sin estado visible

- La request responde `202` y solo muestra un aviso “Envío iniciado”.
- No hay polling ni resumen.
- Ventaja: mínima complejidad.
- Desventaja: el usuario no sabe cuántos se enviaron, omitieron o fallaron.
- No recomendada para un envío masivo institucional.

### Opción D — Bloquear la página completa, no solo el modal

- Overlay de página completa hasta terminar.
- Ventaja: evita acciones concurrentes.
- Desventaja: peor experiencia y sigue sin resolver el cierre de pestaña.
- No recomendada.

**Recomendación:** Opción B. Envío asíncrono real, modal no bloqueante, polling
del estado y `sessionStorage` para recuperar el último job de esa pestaña.

## 5.1 Centro global de tareas reutilizable

No se crea un modal específico para correo. Se crea un **centro de tareas
global** que cualquier operación larga puede usar: envío de correo,
exportaciones, importaciones, generación de reportes u otras tareas futuras.

### Componentes

```text
App
├── Header
├── Sidebar
├── <router-outlet>
└── BackgroundTaskHost   ← modal/píldora global, genérico
```

- `BackgroundTaskCenter` (`providedIn: 'root'`): registro de tareas, estados,
  contadores, polling, minimizar/expandir y persistencia de metadatos.
- `BackgroundTaskHost`: se monta una sola vez en `app.ts`, fuera del router.
- `BackgroundTaskModal`: componente genérico de progreso. No conoce correo,
  exportaciones ni ningún dominio.
- `EmailDispatchService`: adaptador de correo. Solo traduce el estado del job a
  una tarea genérica; no posee modal ni polling propio.

### Modelo genérico

```ts
type BackgroundTaskStatus =
  | 'queued' | 'running' | 'completed' | 'partial' | 'failed';

interface BackgroundTask {
  id: string;
  kind: string;                 // 'email', 'export', 'import', ...
  title: string;
  description?: string;
  status: BackgroundTaskStatus;
  current: number;
  total?: number;
  details: { label: string; value: string }[];
  terminal: boolean;
  resumable?: boolean;
  poll?: () => Observable<BackgroundTaskPatch>;
}
```

API del centro:

```ts
start(task: BackgroundTask): void;
update(id: string, patch: Partial<BackgroundTask>): void;
finish(id: string, status: BackgroundTaskStatus): void;
minimize(): void;
expand(id?: string): void;
dismiss(id: string): void;
```

### Modal genérico

El modal muestra:

- Título y descripción de la tarea.
- Barra de progreso si existe `total`.
- Estado.
- Lista de `details` etiqueta/valor.
- Botones **Minimizar** y **Cerrar**.

No contiene columnas, asunto, HTML de correo ni lógica de destinatarios. El
correo aporta únicamente títulos y contadores:

```ts
details: [
  { label: 'Enviados', value: '...' },
  { label: 'Omitidos', value: '...' },
  { label: 'Fallidos', value: '...' },
]
```

### Múltiples tareas

- El centro mantiene un mapa de tareas activas.
- El host muestra la tarea expandida seleccionada.
- Minimizado muestra una pila de hasta 3 tarjetas o una píldora con el número de
  tareas activas.
- Cada tarea tiene su propio polling; una tarea terminal deja de consultar.
- El correo puede limitar a un envío activo por usuario/proceso con `409`.

### Minimizar y navegar

1. El usuario confirma el envío.
2. `POST /email` responde `202` con `jobId`.
3. `EmailDispatchService` registra una tarea `kind: 'email'` en el centro.
4. El host muestra el modal genérico.
5. **Minimizar** oculta el overlay y deja una tarjeta/píldora fija sin bloquear.
6. Al navegar a otro informe, el host y el centro siguen montados; el polling
   continúa.
7. Cualquier otra operación puede registrar su propia tarea con el mismo modal.

El modal expandido puede bloquear la interacción; la tarjeta minimizada nunca
bloquea la página.

### Confirmación

- La confirmación usa un diálogo reutilizable, no un modal de correo.
- No muestra la plantilla ni el HTML del correo.
- Explica el proceso: primero se carga la lista de alumnos y después se envían
  los correos.
- Indica que el envío continuará en segundo plano y que el progreso se puede
  minimizar.

### Fases visibles

1. **Cargando lista de alumnos**: se obtiene el informe y se construyen los
   destinatarios. La barra es indeterminada.
2. **Enviando correo**: se emiten los correos y avanzan los contadores.
3. **Cancelando envío**: el usuario pidió detener; se cancela el token del job.
4. **Finalizado / Cancelado / Fallido**: estado terminal.

### Cancelación

- El modal muestra **Cancelar envío** junto a **Minimizar** mientras el job está
  activo.
- El botón llama a `POST /email/{jobId}/cancel`.
- El backend cancela el `CancellationTokenSource` del job; el worker detiene el
  envío y marca `CANCELLED`.
- No se revierten los correos ya enviados.
- El modal usa el diseño Bootstrap de SADAlumnos: `modal-header`, `modal-title`,
  `modal-body`, `modal-footer`, `btn-clear`, `btn-save` y `btn-close`.

### Persistencia y otras pestañas

- Las tareas no se restauran desde `sessionStorage`: el estado incluye funciones
  de polling que no se pueden serializar y producía modales congelados.
- El correo guarda solo el `jobId` en `sessionStorage`. Al abrir la pantalla,
  consulta el job real; si está activo lo reanuda, si no existe lo limpia.
- `BroadcastChannel('background-tasks')` sincroniza tareas entre pestañas del
  navegador.
- Cada pestaña monta su propio `BackgroundTaskHost`.
- Un modal no puede moverse físicamente a otra pestaña; se replica el mismo
  estado genérico en cada una.
- Si el API reinicia, el job en memoria desaparece; el adaptador limpia el
  `jobId` y muestra “La tarea anterior ya no está disponible”.
- Varias instancias del API requieren afinidad de sesión o estado compartido, que
  este plan no introduce.

## 6. Contratos

```text
POST /api/reports/academic-enrollment/students-without-photo/email
GET  /api/reports/academic-enrollment/students-without-photo/email/{jobId}
POST /api/reports/academic-enrollment/students-without-photo/email/{jobId}/cancel
```

No hay endpoints de campañas ni historial. La cancelación es por job en memoria.

`POST`:

- No recibe HTML ni cuerpo.
- Usa la plantilla fija.
- Obtiene la lista completa de alumnos sin foto.
- Responde `202` con `jobId` y total.
- `409` si ya hay un envío activo para el mismo usuario/proceso.
- `503` si `SendEmail` está deshabilitado o SMTP no está configurado.

`GET`:

- `404` si el job no existe en esta instancia.
- Estado y contadores sin direcciones de correo.

## 7. Frontend

- Botón **Enviar correo** separado de **Exportar Excel**.
- La confirmación usa un diálogo reutilizable, no un modal de correo.
- La vista previa HTML del texto legacy se muestra en la pantalla o en un
  componente de detalle reutilizable.
- Al confirmar:
  - `POST /email` responde `202` con `jobId`.
  - `EmailDispatchService` registra una tarea genérica en
    `BackgroundTaskCenter`.
  - El host global muestra el progreso; el usuario puede minimizarlo.
  - Se guarda el descriptor de la tarea en `sessionStorage`.
- Al volver a la pantalla o recargar, el centro recupera la tarea y su polling.
- Sin edición de HTML, adjuntos ni programación.
- **Cancelar envío** disponible mientras el job está activo.

## 8. Fases

1. **Configuración SADAlumnos**: `EmailSettings`, `EmailDeliverySettings`,
   `ConfigurationInitialiser`, claves y validaciones.
2. **SMTP**: `IEmailSender`/`SmtpEmailSender` equivalentes a SADAlumnos.
3. **Plantilla**: HTML fijo desde `Button1_Click`.
4. **Destinatarios**: reglas `MailFen`/personal, deduplicación y omitidos.
5. **Cola en memoria**: registry, worker, estados y bloqueo de doble clic.
6. **Endpoints**: `POST` 202 y `GET` de estado.
7. **UI**: centro global de tareas, modal Bootstrap genérico, fases, minimización,
   cancelación, `sessionStorage`, polling y resumen.
8. **Verificación**: mailhog, sin correos reales.

## 9. Pruebas mínimas

- `SendEmail=false`: no envía y responde configuración no disponible.
- Defaults SADAlumnos: mailhog, puerto 1025, from/to esperados.
- AppConfig reemplaza defaults.
- `MailFen` + personal: To/CC correctos.
- Solo personal: To personal, sin CC.
- Sin correos: omitido.
- Deduplicación por `Cod_Alumno`.
- Asunto, from, BCC y HTML exactos.
- `POST` responde `202` sin esperar el envío.
- El worker actualiza estados y contadores.
- Doble clic no crea dos jobs activos.
- Job inexistente: `404`.
- Reinicio del proceso: job perdido, documentado.
- Sin direcciones ni cuerpos en logs.

## 10. Pendientes

- Confirmar si se acepta la limitación de estado en memoria y una sola instancia.
- Confirmar si `sessionStorage` es suficiente para recuperar el job.
- Confirmar si la fecha `13 de junio` queda fija.
- Confirmar si `SendEmail` debe iniciar en `false` hasta validar mailhog.
