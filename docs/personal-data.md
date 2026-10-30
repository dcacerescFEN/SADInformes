# Datos Personales Postgrado

`/postgrado/datos-personales` reemplaza `DatosPersonalesPostgrado.aspx`. Es un
informe **export-only**: filtra por tipo, modalidad y año, y descarga un XLSX; no
muestra los datos personales en pantalla.

## Uso

1. Elegir **Tipo Alumno**, **Modalidad** y **Año Ingreso**. Los tres catálogos
   incluyen `Todos`.
2. Al cambiar el tipo se recargan las modalidades dependientes.
3. Pulsar **Exportar Excel**.
4. Abrir `Datos_Personales_Postgrado.xlsx`.

La grilla de datos personales permanece oculta en la pantalla, como en el legado.

## API

| Ruta base: `/api/reports/postgraduate/personal-data` | Resultado |
| --- | --- |
| `GET /filters?studentType=MBA` | `{ studentTypes, modalities, years }` |
| `GET /download?studentType=Todos&modality=Todos&year=Todos` | XLSX de 26 columnas |
| Permiso de ambas rutas | `REPORT_VIEW_POSTGRADUATE_PERSONAL_DATA` |
| Errores | 400 filtros inválidos o exportación vacía; 401 sin sesión; 403 sin permiso; 503 sanitizado |

Las respuestas autenticadas usan `Cache-Control: no-store`; el archivo lleva
`X-Content-Type-Options: nosniff`.

## Contrato

Los catálogos usan `spRec_TiposAlumnoXEscuela(POSTGRADO, SI)`,
`spRec_EncuestaGraduados_Modalidades(tipo)` y `spRec_Años(SI, Todos)`. El wrapper
`SpRec_AlumnosDatosPersonalesPostgradoAsync` llama a
`spRec_AlumnosDatosPersonalesPostgrado` con `Tipo_Alumno`, `Modalidad` e `Ingreso`
tipados; el SP convierte `Todos` en `%`. No se modifica el procedimiento ni el
esquema.

El XLSX conserva las 26 columnas de la grilla oculta: identificación del alumno,
semestre, persona, tipo, modalidad, situación y motivo, nombres, correos, teléfonos,
empresa, cargo, nacimiento, nacionalidad, sexo, carrera e institución de origen,
fechas de resolución y examen, último período de inscripción y período de examen.

## Verificación y límites

Pruebas Application cubren catálogos dependientes, los 26 campos, `Todos`, tipo
inválido y vacío. HTTP cubre permiso y XLSX. Angular cubre los tres filtros,
recarga de modalidades, descarga, Sin Datos y errores sanitizados.

No sustituyen SQL Server, Excel ni una descarga real desde navegador. No se crean
permisos en Keycloak ni se publican cambios.
