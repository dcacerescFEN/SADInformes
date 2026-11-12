# Cruce de Cursos

Migración de `SAD/Informes/Inscripcion/CruceCursos.aspx` a la ruta
`/academic-enrollment/course-crossing`.

## Comportamiento

- Filtro de **Período**, con los períodos activos de toma de ramos.
- El informe se consulta automáticamente al cargar y al cambiar el período.
- Se muestran los pares de cursos con alumnos inscritos simultáneamente.
- Se agregó visualización en pantalla y exportación XLSX. La pantalla legada
  generaba la descarga directamente, sin mostrar la tabla; se conserva la
  exportación y se hace explícita mediante un botón.

## Columnas

Conserva las ocho columnas legadas:

**Curso 1**, **Cátedra 1**, **Curso 2**, **Cátedra 2**,
**Topes IC**, **Topes IICG**, **Topes Otros**, **Topes Total**.

## Backend

| Elemento | Valor |
|---|---|
| Filtros | `GET /api/reports/academic-enrollment/course-crossing/filters` |
| Informe | `GET .../report?period=...` |
| Exportación | `GET .../export?period=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Informe | `spRec_CruceCursos` |
| Períodos | `spRec_PeriodosActivosTomaRamos` |

La exportación usa el `TextWorkbook` compartido (XLSX, `inlineStr`, sin
fórmulas) y responde `Cache-Control: no-store` y
`X-Content-Type-Options: nosniff`.

Los errores de validación responden `400`; los fallos de SQL, timeout o
configuración responden `503` sin exponer detalles. No se modificaron
procedimientos ni esquema. No se verificó contra SQL Server ni en navegador real.
