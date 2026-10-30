# Profesores confirmados por cátedra

`/postgrado/profesores-vigencias` reemplaza `ProfesoresVigencias.aspx`. Consulta
las vigencias confirmadas de profesores por año y exporta un XLSX real; no
modifica datos ni usa SSRS.

## Uso

1. Elegir un año del catálogo `spRec_Años(NO, Todos)`.
2. Pulsar **Mostrar**. No se consulta al cargar la pantalla.
3. Revisar las 14 columnas del legado, conservando el orden del SP.
4. Pulsar **Descargar Excel** para exportar el resultado mostrado.

La vigencia se muestra como **Vigente** en verde o **No Vigente** en rojo. La
tabla pagina de a 10 filas e incluye el componente Search de SADAlumnos; la
búsqueda filtra todas las columnas visibles y vuelve a la primera página. La
paginación Bootstrap muestra dos números antes y dos después de la página actual,
con `...` cuando hay un salto, además de Anterior/Siguiente. Un
resultado vacío muestra **Sin Datos** y deja la exportación deshabilitada.
Cambiar el año descarta resultados, búsqueda y página, y cancela consultas/descargas anteriores.

## API

| Ruta base: `/api/reports/postgraduate/teacher-validities` | Resultado |
| --- | --- |
| `GET /filters` | `{ success, message, data: { years } }` |
| `GET /?year=2024` | `{ success, message, data: { rows, total } }` |
| `GET /download?year=2024` | XLSX con las 14 columnas |
| Permiso de las tres rutas | `REPORT_VIEW_POSTGRADUATE_TEACHER_VALIDITIES`; administradores existentes conservados |
| Errores | 400 año inválido o exportación vacía; 401 sin sesión; 403 sin permiso; 503 indisponible, sin datos SQL privados |

Las respuestas autenticadas usan `Cache-Control: no-store`; el archivo lleva
`X-Content-Type-Options: nosniff`.

## Contrato y exportación

`SadContextProcedures.SpRec_ProfesoresVigenciasAsync` llama a
`dbo.spRec_ProfesoresVigencias` con `Año` varchar(10) nullable, retorno opcional y
cancelación mediante `SqlQueryAsync`. No se modifica el procedimiento, sus tablas
ni el esquema.

Application conserva el orden del SP y no recalcula horas ni vigencia. Los textos
se recortan, los nulos se muestran vacíos y `HorasDedicadas` usa cultura
invariante sin inventar precisión. El XLSX conserva estas 14 columnas y su orden:

`ID`, `Año`, `Periodo`, `Cod_Catedra`, `Seccion`, `Catedra`, `Tipo_Alumno`,
`Modalidad`, `Promocion`, `Cod_Persona`, `Nombre`, `Email1`, `Horas_Dedicadas`,
`Vigencia`.

Reutiliza `TextWorkbook`: celdas `inlineStr`, sin fórmulas ni dependencias Excel
nuevas. La columna `Vigencia` exporta `Vigente`/`No Vigente`.

## Verificación y límites

Pruebas Application cubren catálogo de años, rechazo de Todos/comodines/fuera de
catálogo antes del SP, orden, 14 campos, horas y vacío. HTTP cubre permiso,
no-store, 400/503 sanitizados, XLSX de 14 columnas y ausencia de adjunto cuando no
hay datos. Angular cubre filtro anual, Mostrar, tabla, badges, cancelación y
descarga del año mostrado.

No sustituyen ejecución contra SQL Server institucional, apertura real en Excel,
descarga desde navegador ni validación de los datos en producción. No se crean
permisos en Keycloak, se reinician procesos ni se publican cambios.
