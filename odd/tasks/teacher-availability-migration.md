# Plan: migración de Disponibilidad Horaria y Observaciones

Estado: Fases 1–4 implementadas.

## 1. Hallazgo

En el menú legado `SAD/Informes/default.aspx` existen dos enlaces con
`Visible="False"`:

```text
Disponibilidad Horaria       → Informes/Inscripcion/DisponibilidadHoraria.aspx
Disponibilidad Observaciones → Informes/Inscripcion/DispObservaciones.aspx
```

El code-behind los muestra si el usuario tiene:

```text
Disponibilidad
Disponibilidad Admin
```

No están en SADInformes porque el catálogo solo incorporó las opciones visibles
normales. No se movieron a otra categoría.

Además, la funcionalidad completa depende de otras dos pantallas:

```text
AdministracionDisponibilidad.aspx
MantenedorDisponibilidades.aspx
```

## 2. Decisión de ubicación

Se migran dentro de SADInformes, en la sección **Inscripción Académica**, porque
el legado las expone desde `Informes/Inscripcion`.

Rutas propuestas:

```text
/academic-enrollment/teacher-availability
/academic-enrollment/teacher-availability-observations
/academic-enrollment/teacher-availability/settings
/academic-enrollment/teacher-availability/{personCode}/edit?period=...
```

Las dos primeras aparecen en la sidebar. Las dos últimas son rutas internas
accesibles desde botones y no se listan como informes.

## 3. Permisos

Se crean permisos nuevos siguiendo `Estandarizacion-Permisos.md`, módulo
`REPORT`:

| Nuevo permiso | Descripción |
|---|---|
| `REPORT_VIEW_AVAILABILITY` | Consulta la disponibilidad horaria y sus observaciones. |
| `REPORT_SEND_AVAILABILITY_EMAIL` | Envía correos individuales o masivos de disponibilidad. |
| `REPORT_EDIT_AVAILABILITY` | Configura el período y edita la disponibilidad de un profesor. |

### Equivalencias legacy

```text
Disponibilidad       > SADInformes  "REPORT_VIEW_AVAILABILITY
                                      REPORT_SEND_AVAILABILITY_EMAIL"

Disponibilidad Admin > SADInformes  "REPORT_VIEW_AVAILABILITY
                                      REPORT_EDIT_AVAILABILITY
                                      REPORT_SEND_AVAILABILITY_EMAIL"
```

Se registran en:

- `Permisos/Equivalencias-Permisos.md`
- `Permisos/Descripciones-Permisos.md`
- `src/Domain/Constants/Permission.cs`
- Roles Keycloak `PERMISSION_REPORT_*`

### Backend

- Lista y observaciones: `REPORT_VIEW_AVAILABILITY`.
- Envío de correo: `REPORT_SEND_AVAILABILITY_EMAIL`.
- Administración y mantenedor: `REPORT_EDIT_AVAILABILITY`.
- Eliminar disponibilidad: `REPORT_EDIT_AVAILABILITY` (el legacy no tiene permiso separado).

`PermissionPolicyProvider` ya resuelve políticas que empiezan con `REPORT_`, por
lo que `REPORT_*` funciona sin cambios en el provider.

### Frontend

`AuthService` solo valida acceso al cliente SADInformes. Se agrega
`hasPermission(permission)` leyendo `resource_access.SADInformes.roles`.

- Sidebar: muestra las dos opciones si el usuario tiene `VIEW` o `EDIT`.
- Botones de correo: requieren `SEND`.
- Administración y mantenedor: requieren `EDIT`.
- Eliminar: requiere `DELETE`.

## 4. Fuentes de datos

### Lista de disponibilidad

```text
spRec_FE_ProgramacionDocente(@Periodo)
spRec_PeriodoDisponibilidad(@Uname)
```

Columnas legadas:

```text
Código | Cátedra | Profesor | Necesarias | Dadas | Disp. | Estado |
Observaciones | acciones
```

`Cod_Profesor` permanece oculto.

### Observaciones

```text
spRec_FE_DisponibilidadObservaciones(@Periodo)
spRec_PeriodosAcademicos(@Limite='20082', @Modo=0, @ConTodos='NO', @Tipo='SEMESTRAL')
```

Columnas:

```text
Cod Profesor | Nombre | Observaciones
```

### Administración del período

```text
spRec_PeriodoDisponibilidad_DatosXPeriodo(@Periodo)
spMod_PeriodoDisponibilidad_DatosXPeriodo(...)
```

Campos: fecha/hora de inicio y término, texto de fecha para correo pendiente,
texto de fecha para correos rezagados y “procesar período seleccionado”.

### Mantenedor de disponibilidad

Usa DocenciaWeb y SP de Sad:

```text
spRec_FE_Catedras
spRec_FE_Modulos
spINS_FE_ModulosCascadaFinal
spMOD_FE_ModulosCascadaFinal
spMOD_FE_TipoModuloCatedras
spELI_Disponibilidades
```

`spRec_FE_Catedras` y `spRec_FE_Modulos` no están en los scripts del repositorio;
antes de implementar hay que confirmar su contrato contra la base o el SAD
legado. `DocenciaWebContext` hoy solo mapea tablas Task, así que el mantenedor
requiere wrappers tipados adicionales o un contrato explícito.

## 5. Correos

Se reutiliza `IEmailSender` y la configuración SMTP de SADAlumnos.

Correos legacy:

- **Bienvenida**: cursos asignados y fecha de disponibilidad.
- **Pendiente/Rezagado**: cursos sin disponibilidad y fecha de rezagados.

Remitente:

```text
secretariaestudios@fen.uchile.cl
```

CC:

```text
secretariaestudios@fen.uchile.cl
```

BCC: clave nueva de AppConfig, por ejemplo
`TeacherAvailabilityEmailBcc`, reemplazando `BBCDisponiblidadHoraria`.

Asunto:

```text
Solicitud Disponibilidad Horaria
```

Acciones:

- Enviar correo a un profesor.
- Mail masivo a todos.
- Mail masivo a pendientes de bienvenida.
- Mail masivo a rezagados.

El envío masivo debe ser asíncrono y usar el centro global de tareas existente,
no bloquear la pantalla. El envío individual puede ser directo con confirmación.

`spMOD_FE_Bienvenida` marca `CorreoBienvenida = 1`; se conserva esa semántica.

## 6. Fases

Fase 1 implementada: permisos de consulta, sidebar, lista y observaciones.
Fase 2 implementada: correos individuales/masivos asíncronos, plantillas legacy, BCC, `spMOD_FE_Bienvenida`, modal global, cancelación y botones diferenciados.
Fase 3 implementada: botón Administración, ruta de configuración, consulta/edición del período, confirmación de activación y pruebas.
Fase 4 implementada: mantenedor de disponibilidad por profesor, edición de modalidad de cursos, módulos cascada y eliminación con confirmación.

Log: Fase 1 completada; fases 2–4 pendientes por escritura en DocenciaWeb/NDB_SEE y contratos no versionados.

### Fase 1 — Solo lectura y permisos

- Catálogo y sidebar con las dos opciones.
- Permisos `REPORT_VIEW_AVAILABILITY` y mapeos legacy.
- `PermissionPolicyProvider` para `REPORT_*`.
- `hasPermission` en frontend.
- Endpoints GET de lista y observaciones.
- Tabla, filtros/periodo activo, contadores Pendientes/Ok, tooltip de
  observaciones.
- Sin correo, sin eliminar, sin editar.

Esta fase ya resuelve que las dos opciones aparezcan y sean consultables.

### Fase 2 — Correos

- `REPORT_SEND_AVAILABILITY_EMAIL`.
- Plantillas HTML de bienvenida y pendiente.
- Envío individual y masivo asíncrono con centro de tareas.
- Confirmaciones y resumen.
- Pruebas con `IEmailSender` falso y mailhog.

### Fase 3 — Administración del período

- `REPORT_EDIT_AVAILABILITY`.
- Formulario de fechas, horas, textos y activación.
- Validación de fechas, horas y período único activo.
- Confirmación explícita antes de activar un período.
- Sin cambios de esquema.

### Fase 4 — Mantenedor y eliminación

- `REPORT_EDIT_AVAILABILITY` para edición y eliminación.
- Editor de módulos por profesor/período.
- Eliminación con confirmación y alcance explícito.
- Wrappers tipados para DocenciaWeb/Sad.
- Pruebas de transacción y rollback.

## 7. Archivos previstos

```text
src/Application/Reports/AcademicEnrollment/TeacherAvailability/
  Commands/
  Queries/
  Common/
src/Web/WebApi/Endpoints/Reports/AcademicEnrollment/TeacherAvailabilityEndpoints.cs
src/Web/WebUI/src/app/academic-enrollment/teacher-availability/
src/Web/WebUI/src/app/academic-enrollment/teacher-availability-observations/
src/Web/WebUI/src/app/academic-enrollment/teacher-availability-settings/
src/Web/WebUI/src/app/academic-enrollment/teacher-availability-editor/
```

No se reutiliza el feature `StudentsWithoutPhoto`; el ownership es Docencia y
Disponibilidad, no Alumnos sin fotos.

## 8. Pruebas

- Permisos: 401 sin sesión, 403 sin `VIEW`, 403 sin `SEND`/`EDIT`/`DELETE`.
- Lista: período activo, columnas, contadores y observaciones.
- Observaciones: período y tres columnas.
- Correo: destinatarios, asunto, CC/BCC, plantilla y fallo parcial.
- Administración: fechas inválidas, hora inválida, activación única.
- Mantenedor: inserción/edición/eliminación y rollback.
- Frontend: sidebar visible solo con permiso, botones ocultos/deshabilitados y
  modal de progreso reutilizable.

## 9. Riesgos y decisiones pendientes

- El mantenedor escribe en DocenciaWeb y Sad; requiere confirmación explícita y
  pruebas de rollback.
- `spRec_FE_Catedras` y `spRec_FE_Modulos` no están versionados en el repo.
- `BBCDisponiblidadHoraria` debe reemplazarse por una clave AppConfig nueva.
- Confirmar si `Disponibilidad` legacy también debe permitir envío individual.
- Confirmar si las observaciones deben ser visibles para `VIEW` o solo `EDIT`.
- Confirmar si el período activo se toma de `spRec_PeriodoDisponibilidad` o de
  `spRec_PeriodosAcademicos`.

## 10. Verificación Fase 1

- WebApi enfocado 4/4 y suite completa 609/609.
- Angular enfocado 8/8 y suite completa 406/406.
- PermissionExporter PASS.
- Build Angular correcto.
- Sin verificación SQL, SMTP ni navegador real.

## 11. Recomendación

Implementar Fase 1 primero, con permisos y solo lectura, para incorporar las dos
opciones al menú sin asumir riesgos de escritura. Después avanzar por fases:
correos, administración y mantenedor.

## Tasks

- [x] T1 — Fase 3: administración del período y botón de administración.
