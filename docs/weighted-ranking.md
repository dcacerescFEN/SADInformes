# Ranking de notas ponderado

`/postgrado/ranking-notas-ponderado` reemplaza `RankingAlumnosGraduados.aspx`
("Ranking Notas Ponderado"). Consulta el procedimiento heredado y descarga un
XLSX real; no modifica datos ni usa SSRS.

## Uso

1. Elegir tipo de alumno, modalidad, año de ingreso y año del cuadro de honor.
   **No existe Todos**: el procedimiento compara valores concretos por igualdad.
2. Pulsar **Mostrar**. No se consulta el ranking al cargar la pantalla.
3. Revisar la posición, foto, nombre, RUT, código de alumno, semestre de ingreso,
   situación, promedio ponderado y percentil conservando el orden del SP.
4. Pulsar **Descargar Excel**. El archivo contiene las ocho columnas de la grilla
   de impresión original y no incluye fotos.
5. **Volver** regresa al inicio.

El tipo de alumno actualiza las modalidades disponibles. Cambiar cualquier filtro
descarta resultados y cancela consultas/descargas anteriores. La exportación usa
la selección del resultado mostrado, no una selección posterior sin consultar. Un
resultado vacío muestra **Sin Datos** y deja la exportación deshabilitada.

Las fotos usan el host fijo heredado `https://fotos.fen.uchile.cl/<rut>.jpg` y
caen a `perfilNN.jpg`. El código abre la ficha moderna de SADAlumnos
`/students/detail/<código codificado>` solo cuando NavigationService tiene un
destino Alumnos autorizado y configurado; si no, se muestra como texto. Este
informe no concede acceso a SADAlumnos.

## API

| Ruta base: `/api/reports/postgraduate/weighted-ranking` | Resultado |
| --- | --- |
| `GET /filters?studentType=MBA` | `{ success, message, data: { studentTypes, modalities, years } }` |
| `GET /?studentType=MBA&modality=PT&cohort=2024&year=2026` | `{ success, message, data: { rows, total } }` |
| `GET /download` con los mismos cuatro filtros | XLSX real; archivo sin fotos |
| Permiso de las tres rutas | `REPORT_VIEW_POSTGRADUATE_WEIGHTED_RANKING`; administradores existentes conservados |
| Errores | 400 filtros inválidos/fuera de catálogo o descarga vacía; 401 sin sesión; 403 sin permiso; 503 indisponible, sin datos SQL privados |

Las respuestas autenticadas de filtros, reporte y Excel usan `Cache-Control: no-store`.
El archivo también lleva `X-Content-Type-Options: nosniff`.

Los catálogos usan `spRec_TiposAlumno(NO, '')`, `spRec_Años(NO, Todos)` y
`spRec_TiposAlumnosModalidades(tipo)`, excluyendo Todos y comodines aun si el SP
los devuelve. Ambos años deben ser cuatro dígitos y pertenecer al catálogo. El
valor `Todos` de la llamada interna a `spRec_Años` conserva su alcance heredado;
no se expone como opción seleccionable.

## Contrato y precisión

El wrapper `SadContextProcedures.SpRec_AlumnosCuadroDeHonorPosgradoAsync` llama a
`dbo.spRec_AlumnosCuadroDeHonorPosgrado` con parámetros nullable tipados:
`Tipo_Alumno`/`Modalidad` varchar(30), `AñoIngreso`/`Año` varchar(4), retorno
opcional y cancelación. Usa `SqlQueryAsync`, no SQL concatenado.

El resultado generado en MappingDB está vacío; el tipo local mapea las columnas
consumidas del SELECT canónico, incluidas `Código` y `Situación`. Application no
recalcula posiciones, orden ni percentiles. El promedio conserva tres decimales y
el percentil cuatro, sin convertirlo a porcentaje ni redondearlo en Angular. Los
nulos se muestran vacíos. El SP mantiene su cálculo heredado de promedios y su
filtro de cursos `CURSADA` del año anterior.

**Límite heredado:** el SP calcula el percentil dividiendo por `@Alumnos - 1`.
Una única fila puede provocar un error SQL; esta migración no cambia la fórmula y
devuelve un 503 seguro si el procedimiento falla.

El XLSX reutiliza `TextWorkbook`, compartido con la auditoría SAD–VTI: celdas
`inlineStr`, sin fórmulas y sin dependencias Excel nuevas. La refactorización de
`SadVtiExcel` conserva las doce columnas y el formato de fecha de su exportación
existente, protegidos por sus pruebas HTTP.

## Verificación y límites

Pruebas Application cubren filtros, rechazo de Todos/comodines, orden, precisión,
nulos y vacío. Pruebas HTTP cubren permisos, no-store, errores sanitizados, XLSX de
ocho columnas y exportación vacía. Angular cubre filtros dependientes, consulta
solo tras Mostrar, fotos con fallback, ficha autorizada, cancelación y descarga.

No sustituyen ejecución contra SQL Server institucional, apertura real en Excel,
descarga desde navegador o acceso al servidor de fotos/SADAlumnos. No se crean
permisos en Keycloak, se reinician procesos ni se publican cambios automáticamente.
