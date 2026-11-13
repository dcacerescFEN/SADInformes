# Reducción de carga

Migración de `SAD/Informes/Inscripcion/ReduccionCarga.aspx` a la ruta
`/academic-enrollment/course-load-reduction`.

## Comportamiento

- Filtro de **Período** con los períodos activos de toma de ramos.
- Consulta automática al cargar y al cambiar el período.
- Muestra los movimientos de eliminación cuyo origen es `REDUCCION DE CARGA`.
- Conserva la exportación XLSX con el nombre `ReduccionCarga<período>.xlsx`.
- Usa `page-header`, `form-section`, `ng-select`, `app-table`, búsqueda, skeleton y
  estado vacío compartidos.

## Columnas

**Período**, **Cátedra**, **Sección**, **Nombre Cátedra**, **Cód. Alumno**,
**Cód. Persona**, **Decreto**, **Nombre**, **Origen**, **Tipo Movimiento**,
**Estado**.

## Backend

| Elemento | Valor |
|---|---|
| Filtros | `GET /api/reports/academic-enrollment/course-load-reduction/filters` |
| Informe | `GET .../report?period=...` |
| Exportación | `GET .../export?period=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Informe | `spRec_ReduccionCargas` |
| Períodos | `spRec_PeriodosActivosTomaRamos` |

La exportación usa el `TextWorkbook` compartido y responde `Cache-Control:
no-store` y `X-Content-Type-Options: nosniff`.

No se modificaron procedimientos ni esquema. No se verificó contra SQL Server ni
en navegador real.
