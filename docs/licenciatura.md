# Próximas Licenciaturas Pregrado

`/postgraduate/undergraduate-licenciatura` reemplaza `LicenciaturaPregrado.aspx`.
Lista alumnos de Pregrado que cumplen los requisitos para obtener la licenciatura,
con búsqueda y exportación.

## Uso

1. Pulsar **Buscar** para ejecutar la consulta; la pantalla no carga datos sola.
2. Filtrar la tabla con el buscador compartido.
3. Pulsar **Exportar** para descargar `ProximasLicenciatura.xlsx`.

Columnas: Código Alumno, Nombre, Nombre Carrera, Grado y Estado. La tabla muestra
además el número de fila, igual que el legado.

## API

| Ruta base: `/api/reports/postgraduate/licenciatura-pregrado` | Resultado |
| --- | --- |
| `GET /report` | filas y total |
| `GET /download` | XLSX de 5 columnas |
| Permiso | `REPORT_VIEW_POSTGRADUATE` |
| Errores | 400 sin datos al exportar; 401/403; 503 sanitizado |

Las respuestas usan `Cache-Control: no-store`; el archivo lleva
`X-Content-Type-Options: nosniff`. No hay filtros porque el legado tampoco los tenía.

## Contrato

`SpList_AlumnosCumpleLicenciaturaAsync` ejecuta
`[SAD].[SpList_AlumnosCumpleLicenciatura]` sin parámetros y mapea las 5 columnas.
La exportación conserva los encabezados originales y el nombre base
`ProximasLicenciatura`, ahora en XLSX en lugar de XLS.

## Defectos legados documentados

- El legado silenciaba las excepciones de búsqueda y exportación; ahora se muestran
  errores saneados sin detalles SQL.
- El SP depende de funciones de SAD para calcular el estado; no se modificó.
- Por su costo, esta consulta usa un timeout de 300 s en lugar del timeout general
  de 60 s; el valor anterior se restaura al terminar.
- El título/ubicación del legado es confuso (opción de Pregrado bajo Postgrado),
  pero se conserva la ruta y el catálogo actuales.

## Verificación y límites

Pruebas Application cubren el mapeo de las 5 columnas. HTTP cubre permiso, XLSX y
el caso sin datos. Angular cubre búsqueda bajo demanda, tabla compartida, filtro,
exportación, `Sin Datos` y errores sanitizados.

No sustituyen SQL Server, Excel ni una prueba en navegador. No se crean permisos en
Keycloak ni se publican cambios.
