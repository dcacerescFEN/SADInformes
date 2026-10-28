# Programación Docente Postgrado — API

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

## Persistencia

Queries y DTO por acción en Application/Reports/Postgraduate/TeachingSchedule; los handlers consumen ISadContextProcedures. Los wrappers canónicos y sus pruebas permanecen en Infrastructure/Data y WebApi.Tests, sin modificar SQL ni acceder a una base real.
