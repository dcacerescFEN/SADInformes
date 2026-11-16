# Disponibilidad Horaria

Migración de `SAD/Informes/Inscripcion/DisponibilidadHoraria.aspx` a
`/academic-enrollment/teacher-availability`.

## Fase implementada

Incluye lista, envío asíncrono de correos, administración del período, edición y
eliminación de disponibilidad.

- Período activo desde `spRec_PeriodoDisponibilidad`.
- Lista desde `spRec_FE_ProgramacionDocente`.
- Contadores de pendientes y OK.
- Tabla con Código, Cátedra, Profesor, Necesarias, Dadas, Disp., Estado y
  Observaciones.
- Búsqueda, skeleton y estado vacío compartidos.
- Los valores legacy con `<br/>` en Código, Cátedra y Disp. se convierten a saltos
  de línea y se muestran en celdas multilínea.

## Backend

```text
GET /api/reports/academic-enrollment/teacher-availability/report
```

Requiere `REPORT_VIEW_AVAILABILITY` y responde `Cache-Control: no-store`.
Errores de validación `400`; fallos SQL/configuración `503` sanitizados.

No se modificaron procedimientos ni esquema. No se verificó contra SQL Server ni
en navegador real.

## Fase 2: correos

- Botones **Enviar a todos**, **Bienvenida pendientes**, **Rezagados** y correo
  por fila, visibles con `REPORT_SEND_AVAILABILITY_EMAIL`.
- El correo por fila es solo icono, con tooltip y `aria-label` **Enviar correo**.
- **Eliminar** usa `spELI_Disponibilidades` con confirmación y permiso
  `REPORT_EDIT_AVAILABILITY` (mismo permiso legacy `Disponibilidad Admin`);
  luego recarga la lista.
- **Observaciones** se muestran como icono con tooltip, igual que el libro del
  SAD legado, en vez de texto multilínea. Si no hay observación, el botón queda
  deshabilitado.
- **Editar** abre el mantenedor del profesor en
  `/academic-enrollment/teacher-availability/{teacherCode}/edit?period=...`.
  Tiene dos tabs: **Cursos y modalidad horaria** y **Módulos de disponibilidad**.
  En cursos, la modalidad se edita con el botón de la columna Acciones y el
  cambio queda en memoria hasta Guardar; si no hay tipo asignado se muestra
  **No Requiere Horario**. En módulos, **Crear módulo** agrega una fila editable
  y su botón Guardar la deja en modo texto. Los selects usan `ng-select` con
  búsqueda. El Guardar inferior queda deshabilitado mientras haya un módulo
  nuevo sin tipo o sin módulo cascada, y persiste todos los cambios.
- Todos los envíos piden confirmación.
- Plantillas legacy de bienvenida y pendiente/rezagado.
- Fechas desde `spRec_PeriodoDisponibilidad_DatosXPeriodo`.
- BCC desde AppConfig `ReportAvailabilityEmailBcc`; default
  `desarrollo@fen.uchile.cl`.
- Los masivos de bienvenida ejecutan `spMOD_FE_Bienvenida` por profesor antes
  del envío.
- El envío es asíncrono y usa el modal global de tareas, con minimizar y cancelar.
- Los botones usan iconos y colores distintos: primario para todos, warning para
  bienvenida pendiente y danger para rezagados.

## Administración del período

- Botón **Administración** en Disponibilidad Horaria, visible con
  `REPORT_EDIT_AVAILABILITY`.
- Ruta interna `/academic-enrollment/teacher-availability/settings`; no aparece en
  la sidebar.
- Formulario: período, fecha y hora de inicio/término, texto de fecha pendiente,
  texto de fecha rezagados y **Procesar período seleccionado**.
- Guardar pide confirmación solo cuando se activa el período seleccionado.
- Al guardar se muestra un toast **Datos guardados** (no un modal bloqueante).
- Usa `spRec_PeriodoDisponibilidad_DatosXPeriodo` y
  `spMod_PeriodoDisponibilidad_DatosXPeriodo`; no cambia el esquema.

## Edición

- Botón **Editar** por fila, visible con `REPORT_EDIT_AVAILABILITY`.
- Ruta: `/academic-enrollment/teacher-availability/{teacherCode}/edit?period=...`.
- Lee cursos con `spRec_FE_Catedras` y módulos con `spRec_FE_Modulos` desde
  DocenciaWeb.
- Guarda modalidad de cursos con `spMOD_FE_TipoModuloCatedras`, módulos existentes
  con `spMOD_FE_ModulosCascadaFinal` y nuevos con
  `spINS_FE_ModulosCascadaFinal`.
- Las observaciones se muestran como icono con tooltip; el botón queda
  deshabilitado cuando no hay observación.
