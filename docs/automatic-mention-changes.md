# Alumnos con cambio automático de mención

Migración de `SAD/Informes/Inscripcion/CambiosMencionAutomaticos.aspx` a la ruta
`/academic-enrollment/automatic-mention-changes`.

## Comportamiento

- Carga los períodos activos de toma de ramos.
- Al cambiar el período vuelve a consultar el informe.
- Conserva las tres columnas legadas:
  **Código**, **Nombre** y **Mención**.
- Usa `app-table`, búsqueda, paginación, skeleton de carga y estado vacío.
- No agrega exportación porque la pantalla legada no la tenía.

## Backend

| Elemento | Valor |
|---|---|
| Filtros de período | `GET /api/reports/academic-enrollment/automatic-mention-changes/filters` |
| Informe | `GET .../report?period=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Origen | `spRec_AlumnosCambioMencionAutomatico` |
| Períodos | `spRec_PeriodosActivosTomaRamos` |

Los errores de validación responden `400`; los fallos de SQL, timeout o
configuración responden `503` sin exponer detalles.

No se modificó el procedimiento ni el esquema. No se verificó contra SQL Server
ni en navegador real.
