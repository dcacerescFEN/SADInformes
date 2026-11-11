# Alumnos que completaron electivos

Migración de `SAD/Informes/Inscripcion/CompletaronElectivos.aspx` a la ruta
`/academic-enrollment/students-completed-electives`.

## Comportamiento

- Carga los períodos activos de toma de ramos.
- Al cambiar el período vuelve a consultar el informe.
- Conserva las seis columnas legadas:
  **Código**, **Nombre**, **Curso**, **Cátedra**, **Decreto** y **Tipo Cátedra**.
- Usa `app-table`, búsqueda, paginación y estado vacío.
- No agrega exportación porque la pantalla legada no la tenía.

## Backend

| Elemento | Valor |
|---|---|
| Filtros de período | `GET /api/reports/academic-enrollment/students-completed-electives/filters` |
| Informe | `GET .../report?period=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Origen | `spRec_AlumnosCompletaronElectivos` |
| Períodos | `spRec_PeriodosActivosTomaRamos` |

Los errores de validación responden `400`; los fallos de SQL, timeout o
configuración responden `503` sin exponer detalles.

No se modificó el procedimiento ni el esquema. No se verificó contra SQL Server
ni en navegador real.
