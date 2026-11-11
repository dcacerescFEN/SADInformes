# Cátedras inscritas sin requisitos

Migración de `SAD/Informes/Inscripcion/InscritasSinRequisitos.aspx` a la ruta
`/academic-enrollment/enrolled-without-requirements`.

## Comportamiento

- Carga los períodos activos de toma de ramos.
- Al cambiar el período vuelve a consultar el informe.
- Conserva las nueve columnas legadas:
  **Código**, **Nombre**, **Cátedra**, **Nombre Cátedra**, **Sección**,
  **Origen**, **Motivo**, **Fecha** y **Usuario**.
- **Motivo** admite multilínea.
- Usa `app-table`, búsqueda, paginación, skeleton y estado vacío.
- No agrega exportación porque la pantalla legada no la tenía.

## Backend

| Elemento | Valor |
|---|---|
| Filtros de período | `GET /api/reports/academic-enrollment/enrolled-without-requirements/filters` |
| Informe | `GET .../report?period=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Origen | `spRec_InscritasSinRequisitos` |
| Períodos | `spRec_PeriodosActivosTomaRamos` |

Los errores de validación responden `400`; los fallos de SQL, timeout o
configuración responden `503` sin exponer detalles.

No se modificó el procedimiento ni el esquema. No se verificó contra SQL Server
ni en navegador real.
