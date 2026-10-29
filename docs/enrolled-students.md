# Alumnos matriculados MBA

`/postgrado/alumnos-matriculados` reemplaza `AlumnosMatriculados.aspx`. Es un
informe **export-only**: conserva la grilla oculta del legado y entrega un XLSX
real, sin mostrar datos personales en pantalla ni modificar datos.

## Uso

1. Elegir modalidad y año.
   - Modalidades fijas del legado: `FOR THE AMERICAS` y `SUNRISE`.
   - Años desde `spRec_Años(NO, Todos)`.
2. Pulsar **Exportar Excel**.
3. Abrir `AlumnosMatriculados.xlsx`.

No hay botón **Mostrar** ni tabla de previsualización. El legado tenía
`grdCuadro Visible="False"` y sólo la usaba para exportar; esta migración conserva
ese comportamiento para no exponer correos, teléfonos ni datos laborales en la
pantalla.

Una exportación vacía responde **Sin Datos** y no genera archivo. Los filtros
inválidos se rechazan antes de llamar al procedimiento.

## API

| Ruta base: `/api/reports/postgraduate/enrolled-students` | Resultado |
| --- | --- |
| `GET /filters` | `{ success, message, data: { modalities, years } }` |
| `GET /download?modality=SUNRISE&year=2024` | XLSX real con 15 columnas |
| Permiso de ambas rutas | `REPORT_VIEW_POSTGRADUATE_ENROLLED_STUDENTS`; administradores existentes conservados |
| Errores | 400 filtros inválidos o exportación vacía; 401 sin sesión; 403 sin permiso; 503 indisponible, sin datos SQL privados |

Las respuestas autenticadas usan `Cache-Control: no-store`; el archivo lleva
`X-Content-Type-Options: nosniff`.

## Contrato y exportación

`SadContextProcedures.SpRec_AlumnosMatriculadosPostgradoAsync` llama a
`dbo.spRec_AlumnosMatriculadosPostgrado` con `Modalidad` varchar(30) y `Anio`
varchar(5) nullable, retorno opcional y cancelación. El SP sigue restringido a
`Tipo_Alumno = 'MBA'` y resuelve el año como `@Anio + 'F'` o `(@Anio + 1) + 'B'`.
No se modifica el procedimiento, sus tablas ni el esquema.

El XLSX conserva las 15 columnas y el orden de la grilla original, incluido el
encabezado legado `Cod_Persona` para el RUT:

`Cod Alumno`, `Semestre_Ingreso`, `Cod_Persona`, `Tipo_Alumno`, `Modalidad`,
`Situacion_Academica`, `Nombres`, `Apellidos`, `Email_Personal`, `Email_Fen`,
`Email_laboral`, `Celular`, `Tel_laboral`, `NomEmpresa`, `CargoEmpresa`.

Reutiliza `TextWorkbook`, compartido con SAD–VTI y el ranking de notas ponderado:
celdas `inlineStr`, sin fórmulas ni dependencias Excel nuevas. Los nulos se
exportan como celdas vacías y los textos se recortan.

## Verificación y límites

Pruebas Application cubren las dos modalidades fijas, catálogo de años, rechazo de
Todos/comodines/fuera de catálogo antes del SP, mapeo de las 15 columnas y vacío.
HTTP cubre permiso, no-store, 400/503 sanitizados, XLSX de 15 columnas y ausencia
de adjunto cuando no hay datos. Angular cubre filtros, exportación directa,
limpieza del blob, Sin Datos y reintento de filtros.

No sustituyen ejecución contra SQL Server institucional, apertura real en Excel,
descarga desde navegador ni validación de los datos personales en producción. No
se crean permisos en Keycloak, se reinician procesos ni se publican cambios.
