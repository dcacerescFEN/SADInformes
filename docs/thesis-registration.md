# Registro de Tesis Postgrado

`/postgrado/registro-tesis` reemplaza `RegistroTesisPostgrado.aspx`. Es un
informe **export-only**: filtra por tipo de alumno, modalidad y año de ingreso, y
descarga un XLSX; la grilla legada estaba oculta y los datos no se muestran en
pantalla.

## Uso

1. Elegir **Tipo Alumno**, **Modalidad** y **Año Ingreso**. Los catálogos incluyen
   `Todos`.
2. Al cambiar el tipo se recargan las modalidades dependientes.
3. Pulsar **Exportar Excel**.
4. Abrir `RegistroTesisPostgrado.xlsx`.

## API

| Ruta base: `/api/reports/postgraduate/thesis-registration` | Resultado |
| --- | --- |
| `GET /filters?studentType=MBA` | `{ studentTypes, modalities, years }` |
| `GET /download?studentType=Todos&modality=Todos&year=Todos` | XLSX de 21 columnas |
| Permiso de ambas rutas | `REPORT_VIEW_POSTGRADUATE_THESIS_REGISTRATION` |
| Errores | 400 filtros inválidos o exportación vacía; 401 sin sesión; 403 sin permiso; 503 sanitizado |

Las respuestas autenticadas usan `Cache-Control: no-store`; el archivo lleva
`X-Content-Type-Options: nosniff`.

## Contrato

Los catálogos usan `spRec_TiposAlumnoXEscuela(POSTGRADO, SI)`,
`spRec_EncuestaGraduados_Modalidades(tipo)` y `spRec_Años(SI, Todos)`. El wrapper
`SpRec_RegistroTesisPostgradoAsync` llama a `spRec_RegistroTesisPostgrado` con
`Tipo_Alumno`, `Modalidad` e `Ingreso` tipados; el SP convierte `Todos` en `%`.

El XLSX conserva las 21 columnas visibles de la grilla oculta: alumno, matrícula,
semestre y decreto de ingreso, persona, tipo, modalidad, apellidos, nombres, datos
del taller AFE, código de cátedra de tesis, período de inscripción, situación y
motivo académico, nombre de tesis, nota, profesores y fecha de examen.

## Defectos legados documentados

- El SP proyecta `et.Cod_Catedra` dos veces; la grilla solo enlazaba una vez. El
  contrato nuevo expone una sola columna.
- El legado cargaba los tipos con `spRec_TiposAlumno('SI','mvenegas')`, un usuario
  fijo. La pantalla nueva usa el catálogo de Postgrado y no incrusta usuarios.
- El SP no tiene `ORDER BY`, por lo que el orden de las filas no está garantizado.
  No se modifica el procedimiento ni el esquema.

## Verificación y límites

Las pruebas Application cubren catálogos dependientes, los 21 campos, `Todos`, tipo
inválido y vacío. HTTP cubre permiso y XLSX. Angular cubre los tres filtros,
recarga de modalidades, descarga, Sin Datos y errores sanitizados.

No sustituyen SQL Server, Excel ni una descarga real desde navegador. No se crean
permisos en Keycloak ni se publican cambios.
