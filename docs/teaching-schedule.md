# Programación Docente Postgrado X Programa y Modalidad

Ruta: `/postgrado/programacion-docente-programa-modalidad`. La pantalla mantiene
los filtros y las columnas del informe legado de
`Informes/Inscripcion/ProgramacionXProgramaModalidad.aspx`, dentro del catálogo
Postgrado actual. Los 30 informes anteriores conservan rutas y referencias.

## Filtros y compatibilidad

| Selector | Valor enviado |
| --- | --- |
| Tipo de período: Bimestre | `PERÍODO` |
| Tipo de período: Semestre | `SEMESTRE` |
| Tipo de período: Año | `AÑO` |
| Programa | `Tipo` de `spRec_TiposAlumnoXEscuela` |
| Modalidad | Valor del catálogo, incluida `Todas` cuando exista |
| Promoción | **`Descripcion`**, mostrando **`Promocion`** |
| Período | Valor del catálogo correspondiente al tipo de período |

Se conserva expresamente el binding de Promoción del legado. No se sustituye la
descripción por el código, no se recorta manualmente ni se cambian el SP o sus
parámetros: el wrapper canónico mantiene `@Promocion varchar(7)` aunque el catálogo
permita descripciones de hasta 50 caracteres. Esta diferencia preexistente es
intencional en la migración y no constituye una corrección de negocio. Las pruebas
comprueban el valor completo antes de la ejecución y los metadatos del parámetro;
no prueban la conversión efectiva del proveedor contra SQL Server real.

La carga inicial selecciona Bimestre y los primeros valores disponibles. Cambiar
tipo de período recarga sus períodos; cambiar programa recarga las modalidades.
Se descartan resultados y exportaciones anteriores al cambiar cualquier filtro;
las respuestas tardías no reemplazan una selección más reciente.

## Datos y exportación

Se muestran las siete columnas del SAD: PeriodoLargo, Curso, Cátedra, Tipo, Ud,
Profesores y Clases. `Ud` conserva números y nulos; el contenido de profesores y
clases se muestra como texto, sin interpretar HTML.

La exportación conserva el nombre base `CursosProfesores`: archivo
`CursosProfesores.csv`, BOM UTF-8, separador punto y coma, escape de comillas y
saltos de línea y protección contra fórmulas. No descarga cuando no hay filas.

## API y permiso

- `GET /api/reports/postgraduate/teaching-schedule/filters?tipoPeriodo=PER%C3%8DODO&tipoAlumno=MBA`.
- `GET /api/reports/postgraduate/teaching-schedule?tipoPeriodo=PER%C3%8DODO&periodo=2024B&tipoAlumno=MBA&modalidad=Todas&promocion=Todas`.

El catálogo devuelve `periodTypes, periods, programs, modalities, promotions`.
Sin programa, devuelve modalidades vacías. El listado usa
`{ success, message, data: { rows, total } }`. Tipo de período, período y programa
son obligatorios para consultar; modalidad y promoción conservan los valores
opcionales y `Todas` del contrato legado. No se aplica la validación genérica de
selecciones concretas de otros informes a estos filtros.

Ambas rutas requieren
`PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHING_SCHEDULE` en el cliente físico
configurado, o los roles REPORT ADMIN/SUPERADMIN existentes. El permiso de entrada
y el de otros informes no conceden acceso. La aplicación no crea ni asigna roles.
Sin sesión devuelve 401; sin permiso, 403; filtros inválidos, 400 antes de leer;
fallos de almacenamiento, 503 sin detalles SQL.

## Persistencia y verificación

Queries y DTO por acción viven en `Application/Reports/Postgraduate/TeachingSchedule`.
Los handlers consumen `ISadContextProcedures`; Infrastructure solo implementa los
wrappers tipados de MappingDB con `SqlQueryAsync<T>`, parámetros, retorno y cancelación.
No hay un adaptador de reportes adicional, tablas nuevas ni SQL ad-hoc en Application.

Se reutilizan `spRec_ProgramacionDocenteXProgramaModalidad`,
`spRec_PeriodosAcademicosXTipo` (POSTGRADO, límite 19000),
`spRec_TiposAlumnoXEscuela` (POSTGRADO, IncluirTodos=NO),
`spRec_TiposAlumnosModalidades` y `spRec_PromocionesPostgradoTodos`.
El SP principal mantiene sus reglas de año/semestre, `LIKE` y `Todas`; excluye
cursos eliminados o de categoría distinta de POSTULACIÓN NORMAL, tipos 4/5 y plan
TULANE, y conserva su cruce con `NDB_Postgrado`. No se reimplementan esas reglas
en C# ni se modifican procedimientos, tablas o registros.

Las pruebas HTTP usan SP dobles. Los contratos SQL se verifican con lectores
interceptados sin abrir conexiones. Las pruebas Angular cubren DOM, filtros,
respuestas fuera de orden, navegación y CSV. La comparación con datos reales,
la conversión varchar del proveedor y las asignaciones de Keycloak requieren un
entorno autorizado; no se ejecutaron pruebas contra SAD real.
