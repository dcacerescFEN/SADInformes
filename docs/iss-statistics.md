# Estadísticas ISS Postgrado

`/postgrado/estadisticas-iss` reemplaza `EstadisticasISS/Resumen.aspx`. Muestra un
resumen estadístico de la evaluación ISS por período y cátedra/sección: la muestra,
las categorías de preguntas con sus alternativas y las observaciones.

## Uso

1. Elegir **Período ingreso** (solo períodos de Postgrado; sin `Todos`).
2. Elegir **Cátedra / Sección** del catálogo dependiente.
3. Pulsar **Mostrar**.
4. Revisar las pestañas **Muestra**, una por categoría y **Observaciones**.

Cada pestaña usa la tabla compartida `app-table` con la paginación estándar. Las
pestañas sin filas no se muestran; si ninguna sección tiene datos, se muestra un
único estado **Sin Datos**. No hay exportación porque la pantalla legada tampoco
la tenía. Los datos se muestran en pantalla.

## API

| Ruta base: `/api/reports/postgraduate/iss-statistics` | Resultado |
| --- | --- |
| `GET /filters?period=2024A` | `{ periods, courseSections }`; sin período usa el primero |
| `GET /summary?period=...&courseSection=ENECO775%2F1` | `{ sample, categories, observations }` |
| Permiso de ambas rutas | `REPORT_VIEW_POSTGRADUATE_ISS_STATISTICS` |
| Errores | 400 filtros inválidos o curso/sección mal formado; 401 sin sesión; 403 sin permiso; 503 sanitizado |

Sin respuestas, `sample` es `null` y las categorías/observaciones quedan vacías; es
un 200 válido, no un error. Las respuestas usan `Cache-Control: no-store`.

## Contrato de datos

- Períodos: `spRec_PeriodosAcademicosProgramacion('20040', 0, 'NO', 'POSTGRADO')`.
- Cátedra/sección: `spRec_EncuestaISSCatedraSeccion(periodo)`.
- Muestra: `spRec_EncuestaISSEstadisticasMuestra(periodo, catedraSeccion)`.
- Categorías: `spRec_EncuestaISSCategorias(periodo, catedraSeccion)` y una llamada
  a `spRec_EvaluacionISSEstadisticas(periodo, catedra, seccion, categoria)` por
  categoría.
- Observaciones: `spRec_EvaluacionISSObservaciones(periodo, catedra, seccion)`.
- Descripciones de categoría: entidad tipada de
  `dbo.Evaluacion_ISS_PostgradoCategorias`, consultada con LINQ; no hay SQL crudo.

Cada pregunta expone `alternative1..5`, `total` y `average`. La UI calcula el
porcentaje de cada alternativa sobre el total de su fila, igual que el legado.

## Defectos legados documentados

- El legado incluía `Todos` en períodos pero su catálogo de cátedras quedaba vacío
  y el valor era el predeterminado. La pantalla nueva excluye `Todos` y selecciona
  el primer período real.
- El legado enlazaba exactamente cuatro controles de categoría y descartaba filas
  adicionales. La pantalla nueva renderiza todas las categorías devueltas, en el
  orden del procedimiento.
- `spRec_EncuestaISSCategorias` y `spRec_EncuestaISSCatedraSeccion` no tienen
  `ORDER BY`; el orden no está garantizado por SQL. No se modifica el esquema ni
  los procedimientos.

## Verificación y límites

Pruebas Application cubren filtros dependientes, muestra, categorías en orden,
preguntas, observaciones, validación y respuesta vacía. HTTP cubre permiso y forma
JSON. Angular cubre filtros, recarga dependiente, render de muestra/categorías/
observaciones, Sin Datos y errores sanitizados.

No sustituyen SQL Server, una sesión Keycloak real ni una prueba visual en
navegador. No se crean permisos en Keycloak ni se publican cambios.
