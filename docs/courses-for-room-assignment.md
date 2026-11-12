# Cursos para asignación de salas

Migración de `SAD/Informes/Inscripcion/CursosSistemaAsignacionSalas.aspx` a la
ruta `/academic-enrollment/courses-for-room-assignment`.

## Comportamiento

- Filtro de **Período**, con los períodos activos de toma de ramos.
- Consulta automática al cargar y al cambiar el período.
- Se agregó visualización en pantalla y exportación XLSX. La pantalla legada
  generaba la descarga directamente, sin mostrar la tabla; se conserva la
  exportación con el nombre `CursosAsignacionSalas<período>.xlsx`.

## Columnas

Conserva las 18 columnas reales del procedimiento:

**Período**, **Cátedra**, **Sección**, **Eliminado**, **Clases**, **Tipo**,
**Vacantes Ofrecidas**, **Vacantes Ocupadas**, **Profesor 1** a **Profesor 10**.

El modelo scaffolded de MappingDB solo declaraba tres columnas; se reemplazó por
el contrato completo del SP.

## Backend

| Elemento | Valor |
|---|---|
| Filtros | `GET /api/reports/academic-enrollment/courses-for-room-assignment/filters` |
| Informe | `GET .../report?period=...` |
| Exportación | `GET .../export?period=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Informe | `spRec_CursosSistemaAsignacionSalas` |
| Períodos | `spRec_PeriodosActivosTomaRamos` |

El SP calcula `VacantesOcupadas` como inscritos aceptados más cinco, tal como
estaba definido; no se modificó ese comportamiento.

La exportación usa el `TextWorkbook` compartido (XLSX, `inlineStr`, sin
fórmulas) y responde `Cache-Control: no-store` y
`X-Content-Type-Options: nosniff`.

Los errores de validación responden `400`; los fallos de SQL, timeout o
configuración responden `503` sin exponer detalles. No se verificó contra SQL
Server ni en navegador real.
