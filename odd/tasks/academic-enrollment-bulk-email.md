# Inscripción académica: correo masivo de Alumnos sin fotos

Implementar el envío asíncrono de correo con el formato de configuración de
SADAlumnos, plantilla legacy y un centro global de tareas reutilizable.

## Specs

- **S1:** `EmailSettings` y `EmailDeliverySettings` siguen el formato de SADAlumnos (`SendEmail`, `EmailFrom`, `EmailTo`, `EmailServer`, `EmailPort`) con defaults mailhog.
- **S2:** Del legacy se conserva el correo: remitente, asunto, BCC, destinatarios (`MailFen` o personal) y el texto de `Button1_Click` como HTML.
- **S3:** El envío usa el mismo permiso `REPORT_ACCESS_REPORT_MODULE`, responde `202` con `jobId` y se ejecuta en un `BackgroundService` sin tabla de campañas.
- **S4:** El frontend usa un centro global de tareas reutilizable, con modal genérico, minimización, polling y persistencia de metadatos.
- **S5:** Pruebas de configuración, destinatarios, plantilla, endpoints, worker y componente global; documentación actualizada.

## Tasks

- T1 | S1 | configuración EmailSettings/EmailDeliverySettings y carga desde AppConfig | completado
- T2 | S2 | plantilla, reglas de destinatarios y servicio SMTP | completado
- T3 | S3 | cola en memoria, worker, registry y endpoints | completado
- T4 | S4 | centro global de tareas, modal reutilizable y UI de correo | completado
- T5 | S5 | pruebas y documentación | completado

## Log

L1. Solicitud: «implementa el plan».
L2. Sin tabla de campañas; estado en memoria; permiso del informe.
L3. Configuración SADAlumnos, no claves legacy.
L4. Modal global reutilizable, no específico de correo.
L5. Implementación: EmailSettings/EmailDeliverySettings, SmtpEmailSender, cola en memoria, EmailDispatchWorker, endpoints preview/start/status, centro global de tareas y UI de confirmación.
L6. Verificación final: WebApi 600/600; Angular 399/399; Node 38/38; build Angular correcto. Sin verificación contra SMTP real ni navegador.
L8. Ajustes UX: modal Bootstrap de SADAlumnos, fases Cargando lista/Enviando correo, cancelación por job y confirmación sin mostrar la plantilla.
L9. Corrección de tarea fantasma al iniciar sesión: no se restauran tareas desde sessionStorage; el adaptador de correo valida el job real al abrir la pantalla y limpia el jobId si no está activo o no existe.
L7. Verificación independiente detectó que la consulta de AppConfig no incluía EmailSettings.Keys; corregido y cubierto por EmailSettingsConfigurationTests. También se agregó prueba de fallo parcial del worker.
