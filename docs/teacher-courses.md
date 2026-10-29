# Cursos Profesores Postgrado

Informe migrado desde `SAD/Informes/Postgrado/CursosProfesoresPostgrado.aspx`.
Reemplaza la pantalla Web Forms sin cambiar sus datos de origen.

## Filtros

| Filtro | Origen |
| --- | --- |
| Período (año) | `spRec_PeriodosAcademicosXTipo` con `AÑO`, `POSTGRADO` y límite `19000` |
| Departamento/Escuela | `spRec_DocentesDepartamentos` con `IncluirTodos = NO` y el usuario de AppConfig |

La consulta usa `spRec_EvaPostgradoCursosDocentes2(período, departamento)` sin cambios en el
procedimiento: se conservan sus exclusiones, sus trece preguntas y el promedio calculado.

## Alcance por departamento

`spRec_EvaPostgradoCursosDocentes2` no valida el departamento: solo lo filtra. Por eso el
backend resuelve los departamentos visibles con el usuario SAD configurado en
`PostgraduateSettings.User` (clave `PostgraduateUser` de AppConfig, `mvenegas` cuando el
valor es nulo) y rechaza con `400` cualquier departamento ajeno a esa lista antes de
ejecutar el informe. El cliente nunca envía ni elige el usuario SAD.

Si el usuario configurado falta, está vacío o excede 20 caracteres, la API responde `503`
con un mensaje genérico y no consulta datos.

## Columnas y exportación

Se muestran las cinco columnas originales: `Cod_Profesor`, `Nombre`,
`#CatedrasEvaluadas`, `Promedio Ev. Docente Profesor` y `Tipo AQ_PQ_O`.

El promedio se envía como texto decimal con coma (`5,250000`), conservando la escala
`decimal(19,6)` que devuelve el procedimiento: no se redondea ni se recortan ceros.
El valor nulo se muestra vacío. Al ser un texto formateado, la columna no ofrece
ordenamiento en la tabla.

La pantalla legada exportaba desde el botón Mostrar con el nombre
`CursosProfesores_Postgrado.xls`. La migración separa la consulta de la exportación y
genera `CursosProfesores_Postgrado.csv` con BOM UTF-8, separador `;`, escape de comillas y
protección contra fórmulas de planilla.

## API

| Ruta | Descripción |
| --- | --- |
| `GET /api/reports/postgraduate/teacher-courses/filters` | Años y departamentos disponibles |
| `GET /api/reports/postgraduate/teacher-courses?periodo=2024&departamento=Ingeniería` | Filas del informe |

Ambas rutas exigen `REPORT_VIEW_POSTGRADUATE_TEACHER_COURSES` (o los roles
`ROLE_REPORT_ADMIN` / `ROLE_REPORT_SUPERADMIN`). Los errores de entrada responden `400`
y los fallos de almacenamiento `503`, ambos sin detalles SQL.

## Verificación

```sh
dotnet build SADInformes.slnx
dotnet test tests/WebApi.Tests/WebApi.Tests.csproj
node --test tests/*.test.mjs
cd src/Web/WebUI && pnpm test && pnpm build
```

Las pruebas cubren permisos, alcance departamental, filtros inválidos, mapeo de columnas
con nulos, errores sanitizados, respuestas fuera de orden y el CSV exportado.
