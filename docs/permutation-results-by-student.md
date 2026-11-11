# Resultados de permuta por alumno

Migración de `SAD/Informes/Inscripcion/ResPermutaXAlumno.aspx` a la ruta
`/academic-enrollment/permutation-results-by-student`.

## Comportamiento

- Carga los períodos activos de toma de ramos.
- Al seleccionar un período carga sus rangos de proceso de permuta.
- Al seleccionar un rango completa **Desde** y **Hasta** con las fechas del proceso.
- Permite ajustar las fechas antes de actualizar.
- Conserva las ocho columnas legadas:
  **Código**, **Nombre**, **Tipo Alumno**, **Cátedra**, **Nom.Cátedra**,
  **Desde**, **Hacia** y **Resultado**.
- Usa `app-table`, búsqueda, paginación y estado vacío.
- No agrega exportación porque la pantalla legada no la tenía.

## Backend

| Elemento | Valor |
|---|---|
| Filtros de período | `GET /api/reports/academic-enrollment/permutation-results-by-student/filters` |
| Rangos de proceso | `GET .../processes?period=...` |
| Informe | `GET .../report?period=...&from=...&to=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Origen | `spRec_ResPermutaXAlumno` |
| Períodos | `spRec_PeriodosActivosTomaRamos` |
| Rangos | `spRec_ProcesosPeriodos` con `PERMUTA` |

Las fechas se envían con hora (`yyyy-MM-ddTHH:mm`) para conservar el rango del
proceso. Los errores de validación responden `400`; los fallos de SQL, timeout o
configuración responden `503` sin exponer detalles.

No se modificó el procedimiento ni el esquema. No se verificó contra SQL Server
ni en navegador real.
