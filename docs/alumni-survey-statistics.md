# Estadísticas Encuesta Alumni

`/postgraduate/alumni-survey-statistics` reemplaza `EstadisticaEncuestaAlumni.aspx`.
Es un informe **export-only**: muestra el diccionario de preguntas y descarga las
respuestas en Excel; las respuestas individuales no se muestran en pantalla.

## Uso

1. Revisar y filtrar el **Diccionario de preguntas** (30 códigos, `P1` a `P14`) con el buscador compartido.
2. Pulsar **Exportar Excel**.
3. Abrir `EncuestaAlumniPostgrado.xlsx`.

No hay filtros, igual que el legado.

## API

| Ruta base: `/api/reports/postgraduate/alumni-survey-statistics` | Resultado |
| --- | --- |
| `GET /download` | XLSX de 32 columnas |
| Permiso | `REPORT_VIEW_POSTGRADUATE_ALUMNI_SURVEY_STATISTICS` |
| Errores | 400 sin datos; 401 sin sesión; 403 sin permiso; 503 sanitizado |

La respuesta usa `Cache-Control: no-store`; el archivo lleva
`X-Content-Type-Options: nosniff`.

## Contrato

`SpRec_EstadisticaEncuestaAlumniAsync` ejecuta `spRec_EstadisticaEncuestaAlumni`
sin parámetros y mapea 32 columnas: `Cod_Alumno`, los 30 campos de preguntas y
`Fecha`. La grilla legada estaba oculta y se conserva como exportación únicamente.

El diccionario visible usa la tabla compartida `app-table`. La exportación conserva
los encabezados originales y el nombre base `EncuestaAlumniPostgrado`, ahora en
XLSX en lugar de XLS.

## Defectos legados documentados

- `P10` se exporta con su valor numérico original; el SP no lo traduce a `SI`/`NO`,
  a diferencia de `P11`–`P13`.
- El SP no tiene `ORDER BY`, por lo que el orden de las respuestas no está
  garantizado.
- El título HTML legado decía “Estadísticas Encuesta Entrada”, aunque la pantalla
  corresponde a Alumni.

No se modifica el procedimiento ni el esquema.

## Verificación y límites

Las pruebas Application cubren el mapeo de las 32 columnas. HTTP cubre permiso,
XLSX y el caso sin datos. Angular cubre el diccionario compartido, la descarga,
`Sin Datos` y errores sanitizados.

No sustituyen SQL Server, Excel ni una descarga real desde navegador. No se crean
permisos en Keycloak ni se publican cambios.
