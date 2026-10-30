# Reporte de áreas y cátedras

`/postgrado/alumnos-por-area` reemplaza `ReporteAreasCatedras.aspx`. Consulta
cátedras y áreas para dos años y exporta un XLSX real; no modifica datos ni usa
SSRS.

## Uso

1. Elegir **Año** y **Otro Año** desde `spRec_Años(NO, Todos)`. Ambos son
   obligatorios y pueden ser iguales.
2. Pulsar **Mostrar**. No se consulta al cargar la pantalla.
3. Revisar las 28 columnas originales en una tabla horizontal.
4. Pulsar **Descargar Excel** para exportar el resultado mostrado.

La tabla pagina de a 10 filas e incluye el componente Search de SADAlumnos; la
búsqueda filtra todas las columnas visibles y vuelve a la primera página. La
paginación Bootstrap muestra dos números antes y dos después de la página actual,
con `...` cuando hay saltos, además de Anterior/Siguiente. Un resultado vacío
muestra **Sin Datos** y deja la exportación deshabilitada. Cambiar cualquiera de
los años descarta resultados, búsqueda y página, y cancela consultas/descargas anteriores.

## API

| Ruta base: `/api/reports/postgraduate/area-courses` | Resultado |
| --- | --- |
| `GET /filters` | `{ success, message, data: { years } }` |
| `GET /?year1=2024&year2=2026` | `{ success, message, data: { rows, total } }` |
| `GET /download?year1=2024&year2=2026` | XLSX con las 28 columnas |
| Permiso de las tres rutas | `REPORT_VIEW_POSTGRADUATE_AREA_COURSES`; administradores existentes conservados |
| Errores | 400 años inválidos o exportación vacía; 401 sin sesión; 403 sin permiso; 503 indisponible, sin datos SQL privados |

Las respuestas autenticadas usan `Cache-Control: no-store`; el archivo lleva
`X-Content-Type-Options: nosniff`.

## Contrato y exportación

`SadContextProcedures.SpRec_CatedrasAreas_ReportePostgradoAsync` llama a
`dbo.spRec_CatedrasAreas_ReportePostgrado` con `Año1`/`Año2` varchar(10)
nullable, retorno opcional y cancelación mediante `SqlQueryAsync`. No se modifica
el procedimiento, sus tablas ni el esquema.

Application conserva el orden del SP y no recalcula promedios, alumnos,
observaciones ni categorizaciones. Los textos se recortan y los nulos se muestran
vacíos. El XLSX conserva las 28 columnas y el orden de la grilla original:

`ID`, `Area_Catedra`, `Area`, `Periodo`, `Cod_Catedra`, `Seccion`, `Creditos`,
`Nombre_Profesor`, `Cod_Persona`, `Tipo_Profesor`, `Inicio_Catedra`,
`Termino_Catedra`, `Tipo_Alumno`, `Modalidad`, `Promocion`, `Nombre_Catedra`,
`TipoCatedra`, `Tipo_Horario`, `AQPQ`, `Categorización_Docente`, `Confirmado`,
`HorasDedicadas`, `Alumnos_Totales`, `Alumnos_Postgrado`, `Alumnos_Pregrado`,
`Promedio_Ev_Docente`, `Observaciones Curso`, `Usuario_Observ`.

Reutiliza `TextWorkbook`: celdas `inlineStr`, sin fórmulas ni dependencias Excel
nuevas. `Confirmado` conserva el texto del SP (`Confirmado`/`No Confirmado`) y el
promedio de evaluación conserva el formato regional entregado por SQL Server.

## Verificación y límites

Pruebas Application cubren catálogo de años, rechazo de Todos/comodines/fuera de
catálogo antes del SP, orden, 28 campos y vacío. HTTP cubre permiso, no-store,
400/503 sanitizados, XLSX de 28 columnas y ausencia de adjunto cuando no hay
datos. Angular cubre dos filtros anuales, Mostrar, tabla, cancelación y descarga
de la selección mostrada.

No sustituyen ejecución contra SQL Server institucional, apertura real en Excel,
descarga desde navegador ni validación de los datos en producción. No se crean
permisos en Keycloak, se reinician procesos ni se publican cambios.
