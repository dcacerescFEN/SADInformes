# Cátedras inscritas ya aprobadas

Migración de `SAD/Informes/Inscripcion/InscritasYaAprobadas.aspx` a la ruta
`/academic-enrollment/enrolled-approved-courses`.

## Comportamiento

- Carga los períodos activos de toma de ramos.
- Al cambiar el período vuelve a consultar el informe.
- Conserva las seis columnas legadas:
  **Código**, **Nombre**, **Cátedra**, **Nombre Cátedra**, **Sección** y **Origen**.
- Usa `app-table`, búsqueda, paginación, skeleton de carga y estado vacío.
- No agrega exportación porque la pantalla legada no la tenía.

## Backend

| Elemento | Valor |
|---|---|
| Filtros de período | `GET /api/reports/academic-enrollment/enrolled-approved-courses/filters` |
| Informe | `GET .../report?period=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Origen | `spRec_InscritasYaAprobadas` |
| Períodos | `spRec_PeriodosActivosTomaRamos` |

Los errores de validación responden `400`; los fallos de SQL, timeout o
configuración responden `503` sin exponer detalles.

No se modificó el procedimiento ni el esquema. No se verificó contra SQL Server
ni en navegador real.
