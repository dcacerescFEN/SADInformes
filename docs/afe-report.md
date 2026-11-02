# Reporte Avances AFE

`/postgrado/avances-afe` reemplaza `ReporteAFE.aspx`. Consulta el avance de tareas
AFE por período, tipo de alumno, modalidad, curso/sección y número de avance, y
exporta el resultado en Excel.

## Uso

1. Elegir **Período**, **Tipo Alumno** y **Modalidad**; los cursos se recargan en
   cascada.
2. Elegir **Cursos** y **Número Avance** (`Todos` cuando hay más de uno).
3. Pulsar **Buscar**.
4. Filtrar la tabla con el buscador compartido o descargar el Excel.

Las columnas son las 12 del legado: Avance Afe, Nombre Avance, Fecha Creación,
Fecha Entrega Programada, Nombre Profesor, Cátedra, Nombre Alumno, Grupo, Fecha
Entrega Real, Evaluación, Fecha Evaluación y Obs. Evaluación.

## API

| Ruta base: `/api/reports/postgraduate/afe-report` | Resultado |
| --- | --- |
| `GET /filters?period=&studentType=&modality=` | períodos, tipos, modalidades y cursos |
| `GET /advances?period=&course=COD/SECC` | avances AFE del curso; `Todos` si hay más de uno |
| `GET /report?period=&course=&advance=` | filas del reporte |
| `GET /download?period=&course=&advance=` | XLSX `Reporte nivel curso AFE - coordinacion.xlsx` |
| Permiso | `REPORT_VIEW_POSTGRADUATE_AFE_REPORT` |
| Errores | 400 filtros/curso/avance inválidos; 401/403; 503 sanitizado |

## Dependencias

El reporte cruza dos bases:

- **SAD**: períodos, tipos, modalidades, cursos, alumnos, inscripciones y los SPs
  `spRec_AfePorPeriodoTipoAlumno` y `SpRec_ObtenerTareasAFEReporte`.
- **DocenciaWeb**: `TaskM`, `TaskTipo`, `TaskEntregas`, `TaskGrupo` y
  `TaskGrupoAlumno`, leídos mediante `IDocenciaWebContext` y LINQ.

Configurar `ConnectionStrings:DocenciaWeb` en el entorno. Si falta, las rutas
responden 503 sanitizado. No se guardan credenciales en el repositorio.

Los identificadores de tareas (`TaskM.Id`, `Task_id`, `Grupo_id`), el `Id` de
`SpRec_ObtenerTareasAFEReporte` y el `Id` de `spRec_AfePorPeriodoTipoAlumno` son
`bigint`; se mapean como `long` para evitar `InvalidCastException`.

## Defectos legados documentados

- El legado usaba un catálogo de tipos con usuario fijo; la pantalla nueva usa el
  catálogo de Postgrado.
- El legado silenciaba excepciones de SQL y dejaba la grilla vacía; ahora los
  errores se sanean y se informan sin detalles de base de datos.
- El SP de tareas devuelve todas las tareas del período; la aplicación filtra el
  curso y calcula `OrdenAfe` sobre las tareas del curso, conservando el criterio
  legado (la tarea más nueva tiene el número mayor).
- Las entregas de alumnos que no aparecen en la nómina del curso se omiten en vez
  de provocar el fallo de `First()` del legado.

No se modifican SPs ni esquema.

## Verificación y límites

Pruebas Application cubren filtros, avances, mapeo de tareas/alumnos/entregas y
el caso sin alumnos. HTTP cubre permiso, nombre y 12 columnas del XLSX. Angular
cubre filtros en cascada, tabla compartida, búsqueda, exportación, vacío y errores.

No sustituyen SQL Server, DocenciaWeb real, Excel ni una prueba en navegador. No
se crean permisos en Keycloak ni se publican cambios.
