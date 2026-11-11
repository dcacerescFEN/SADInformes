# Resultados de postulación y agrega por curso

Migración de `SAD/Informes/Inscripcion/ResPostulaAgregaXCurso.aspx` en sus dos
variantes:

- `origen=INSCRIPCION`: `/academic-enrollment/postulation-results-by-course`
- `origen=AGREGA`: `/academic-enrollment/add-results-by-course`

## Comportamiento

- Carga los períodos activos de toma de ramos.
- Al seleccionar un período carga los rangos del proceso correspondiente.
- Al seleccionar un rango completa **Desde** y **Hasta** con las fechas del proceso.
- Permite ajustar las fechas antes de actualizar.
- Conserva las 17 columnas visibles del SAD, incluidos los encabezados repetidos
  `Acep. Post.` y `Disp.` del diseño legado.
- Usa la tabla compartida, búsqueda, paginación y estado vacío.
- No agrega exportación porque la pantalla legada no la tenía.

## Backend

| Elemento | Valor |
|---|---|
| Filtros de período | `GET /api/reports/academic-enrollment/postulation-results-by-course/filters` |
| Rangos de proceso | `GET .../processes?origin=INSCRIPCION&period=...` |
| Informe | `GET .../report?origin=INSCRIPCION&period=...&from=...&to=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Origen | `spRec_ResPostulaAgregaXCurso` |
| Períodos | `spRec_PeriodosActivosTomaRamos` |
| Rangos | `spRec_ProcesosPeriodos` |

El origen se normaliza a `INSCRIPCION` o `AGREGA`. Las fechas se envían con hora
(`yyyy-MM-ddTHH:mm`) para conservar el rango del proceso. Los errores de
validación responden `400`; los fallos de SQL, timeout o configuración responden
`503` sin exponer detalles.

## Pendiente

El detalle emergente por curso (`ResPostulaAgregaXCursoAlumnos.aspx`) no forma
parte de esta migración. Los conteos se muestran como valores, sin enlaces al
detalle.

No se modificó el procedimiento ni el esquema. No se verificó contra SQL Server
ni en navegador real.
