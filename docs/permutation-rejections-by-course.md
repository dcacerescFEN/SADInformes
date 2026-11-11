# Permutas rechazadas por vacantes por curso

Migración de `SAD/Informes/Inscripcion/ResPermutaXCursoVacantes.aspx` a la ruta
`/academic-enrollment/permutation-rejections-by-course`.

## Comportamiento

- Carga los períodos activos de toma de ramos.
- Al cambiar el período vuelve a consultar el informe.
- Conserva las cinco columnas legadas:
  **Cátedra**, **Nom.Cátedra**, **Sección**, **Tipo Alumno** y **Rechazos**.
- **Rechazos** se alinea a la derecha, como en el SAD.
- Usa `app-table`, búsqueda, paginación y estado vacío.
- No agrega exportación porque la pantalla legada no la tenía.

## Backend

| Elemento | Valor |
|---|---|
| Filtros de período | `GET /api/reports/academic-enrollment/permutation-rejections-by-course/filters` |
| Informe | `GET .../report?period=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Origen | `spRec_ResPermutaXCursoVacantes` |
| Períodos | `spRec_PeriodosActivosTomaRamos` |

Los errores de validación responden `400`; los fallos de SQL, timeout o
configuración responden `503` sin exponer detalles.

No se modificó el procedimiento ni el esquema. No se verificó contra SQL Server
ni en navegador real.
