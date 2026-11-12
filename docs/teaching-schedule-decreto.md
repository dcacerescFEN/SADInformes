# Programación Docente por decreto

Migración de `SAD/Informes/Inscripcion/ProgramacionDocenteDecreto.aspx` a cuatro
rutas de Inscripción Académica:

| Ruta | Decreto | Título |
|---|---|---|
| `/academic-enrollment/teaching-schedule-ic-old-plan` | `00921/89%` | Ingeniería Comercial Plan Antiguo |
| `/academic-enrollment/teaching-schedule-ic-new-plan` | `009615/2003%` | Ingeniería Comercial Plan Nuevo |
| `/academic-enrollment/teaching-schedule-iicg-old-plan` | `00433/89%` | Ingeniería en Información y Control de Gestión Plan Antiguo |
| `/academic-enrollment/teaching-schedule-iicg-new-plan` | `009538/2003%` | Ingeniería en Información y Control de Gestión Plan Nuevo |

## Comportamiento

- Filtro de **Período**, obtenido de `spRec_PeriodosAcademicosProgramacion` para
  Pregrado; se selecciona el más reciente.
- El título del plan lo entrega el backend a partir del slug.
- El slug se valida contra un catálogo cerrado: el endpoint no acepta decretos
  arbitrarios.
- El informe se consulta automáticamente al cargar y al cambiar el período.

## Columnas

Conserva las nueve columnas legadas:

**Curso**, **Cátedra**, **Nombre**, **Tipo**, **UD**, **Mención**,
**Requisitos**, **Profesor(es)**, **Clases**.

No se agregó exportación porque la pantalla legada no la tenía.

## Backend

| Elemento | Valor |
|---|---|
| Filtros | `GET /api/reports/academic-enrollment/teaching-schedule-decreto/filters?slug=...` |
| Informe | `GET .../report?slug=...&period=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Informe | `spRec_ProgramacionDocenteXDecreto` |
| Períodos | `spRec_PeriodosAcademicosProgramacion` |

El SP se invoca con `@TipoPeriodo = 'PERÍODO'` y `@Escuela = '%'`, igual que los
valores por defecto legados. No se modificaron procedimientos ni esquema.

Los errores de validación responden `400`; los fallos de SQL, timeout o
configuración responden `503` sin exponer detalles. No se verificó contra SQL
Server ni en navegador real.
