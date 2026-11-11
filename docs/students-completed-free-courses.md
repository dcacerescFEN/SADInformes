# Alumnos que completaron libres

Migración de `SAD/Informes/Inscripcion/CompletaronLibres.aspx` a la ruta
`/academic-enrollment/students-completed-free-courses`.

## Comportamiento

- Carga los períodos activos de toma de ramos.
- Al cambiar el período vuelve a consultar el informe.
- Conserva las cuatro columnas legadas:
  **Código**, **Nombre**, **Curso** y **Cátedra**.
- Usa `app-table`, búsqueda, paginación y estado vacío.
- No agrega exportación porque la pantalla legada no la tenía.

## Backend

| Elemento | Valor |
|---|---|
| Filtros de período | `GET /api/reports/academic-enrollment/students-completed-free-courses/filters` |
| Informe | `GET .../report?period=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Origen | `spRec_AlumnosCompletaronLibres` |
| Períodos | `spRec_PeriodosActivosTomaRamos` |

Los errores de validación responden `400`; los fallos de SQL, timeout o
configuración responden `503` sin exponer detalles.

No se modificó el procedimiento ni el esquema. No se verificó contra SQL Server
ni en navegador real.
