# Estadísticas Encuesta Entrada

`/postgrado/estadisticas-encuesta-entrada` reemplaza
`EstadisticaEncuestaEntrada.aspx`. Es un informe **export-only**: muestra el
diccionario de preguntas y descarga las respuestas crudas en Excel; no expone la
grilla de datos personales en pantalla ni modifica datos.

## Uso

1. Revisar el diccionario de 40 preguntas con sus códigos y descripciones.
2. Pulsar **Descargar Excel**.
3. Abrir `EncuestaEntradaPostgrado.xlsx`.

No hay filtros, búsqueda ni paginación porque el legado tampoco los tiene. La
grilla de respuestas estaba oculta (`Visible="False"`) y se conserva ese
comportamiento.

## API

| Ruta base: `/api/reports/postgraduate/entry-survey-statistics` | Resultado |
| --- | --- |
| `GET /` | `{ success, message, data: { rows, total } }` |
| `GET /download` | XLSX con 44 columnas visibles |
| Permiso de ambas rutas | `REPORT_VIEW_POSTGRADUATE_ENTRY_SURVEY_STATISTICS` |
| Errores | 400 exportación vacía; 401 sin sesión; 403 sin permiso; 503 indisponible sanitizado |

Las respuestas autenticadas usan `Cache-Control: no-store`; el archivo lleva
`X-Content-Type-Options: nosniff`.

## Contrato y defecto heredado

`SadContextProcedures.SpRec_EstadisticaEncuestaEntradaAsync` ejecuta
`dbo.spRec_EstadisticaEncuestaEntrada` sin parámetros con `SqlQueryAsync`, igual
que los demás SP. El procedimiento debe devolver las 44 columnas visibles; la
corrección de los alias duplicados `Q3_1`/`Q3_2` queda del lado del SP. El resto de
las respuestas se conserva como texto, incluidas las escalas ya traducidas por SQL.

El XLSX reutiliza `TextWorkbook` y conserva el orden de la grilla oculta:
`Cod_Alumno`, `Tipo_Alumno`, `Modalidad`, `Q1`, `Q2_1` a `Q2_8`, `Q3_1` a
`Q3_7`, `Q4_1` a `Q4_8`, `Q5`, `Q5_otro`, `Q6`, `Q7`, `Q8`, `Q9`, `Q9_otro`,
`Q10`, `Q11`, `Q11_otro`, `Q12`, `Q13`, `Q14`, `Q15`, `Q16`, `Q17` y `Fecha`.

## Verificación y límites

Pruebas Application cubren el mapeo de 44 campos y el resultado vacío. HTTP cubre
permiso y exportación con 44 columnas. Angular cubre el diccionario de 40
preguntas, descarga, Sin Datos y errores sanitizados.

No sustituyen una ejecución real contra SQL Server, la apertura del archivo en
Excel ni una descarga desde navegador. No se crean permisos en Keycloak ni se
publican cambios.
