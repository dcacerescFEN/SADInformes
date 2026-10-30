# Evaluación Docente/Curso

`/postgrado/evaluacion-docente-curso` reemplaza `Eva_CursosProfesores.aspx`. Es un
informe **export-only**: filtra por año y tipo, y descarga un XLSX; no muestra la
grilla de respuestas en pantalla.

## Uso

1. Elegir un año desde `spRec_Años(NO, Todos)`.
2. Elegir el tipo:
   - `EX`: Executive, evaluación de curso con 4 preguntas.
   - `FT`: Full Time, evaluación de curso con 23 preguntas.
3. Pulsar **Exportar Excel**.
4. Abrir `Evaluacion_Docente_Curso.xlsx`.

No hay tabla visible ni paginación. El legado mantenía las dos grillas ocultas y
sólo las usaba para exportar; se conserva ese comportamiento.

## API

| Ruta base: `/api/reports/postgraduate/course-teacher-evaluation` | Resultado |
| --- | --- |
| `GET /filters` | `{ years, types }` con `EX` y `FT` |
| `GET /download?year=2024&type=EX` | XLSX de 31 columnas para Executive |
| `GET /download?year=2024&type=FT` | XLSX de 50 columnas para Full Time |
| Permiso de ambas rutas | `REPORT_VIEW_POSTGRADUATE_COURSE_TEACHER_EVALUATION` |
| Errores | 400 año/tipo inválido o exportación vacía; 401 sin sesión; 403 sin permiso; 503 sanitizado |

Las respuestas autenticadas usan `Cache-Control: no-store`; el archivo lleva
`X-Content-Type-Options: nosniff`.

## Contrato

`SpRec_EvaPostgradoCursosDocentesAsync` y su variante
`...FullTimeAsync` ejecutan el mismo SP
`spRec_EvaPostgradoCursosDocentes(@Año, @Tipo)` con wrappers tipados. El SP conserva
sus dos formas de resultado, exclusiones y cálculos; no se modifica.

El XLSX conserva las columnas de cada grilla oculta y usa `TextWorkbook`:
- Executive: `Periodo`, `Cod_Catedra`, `Cod_Seccion`, `Cod_Profesor`, `Nombre`,
  `Nombre Catedra`, `Tipo Alumno`, `Modalidad`, `Inscritos`, `Contestadas Curso`,
  `PorcentajeCurso`, `ContestadasDocente`, `PorcentajeDocente`, `C1_1` a `C1_4`,
  `P1_1` a `P1_13` y `Nota`.
- Full Time: las mismas columnas base, `C1_1` a `C1_23`, `P1_1` a `P1_13` y `Nota`.

Los decimales se exportan con dos decimales invariantes; nulos quedan vacíos.

## Verificación y límites

Pruebas Application cubren las dos formas, los filtros inválidos y la exportación
HTTP. Angular cubre filtros, descarga, Sin Datos y errores sanitizados.

No sustituyen SQL Server, Excel o una descarga real desde navegador. No se crean
permisos en Keycloak ni se publican cambios.
