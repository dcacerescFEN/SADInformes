# Alumnos sin prioridad

Migración de `SAD/Informes/Inscripcion/AlumnosSinPrioridad.aspx` a la ruta
`/academic-enrollment/students-without-priority`.

## Comportamiento

- Carga el informe automáticamente al abrir la ruta.
- Presenta las cuatro columnas originales: **Código**, **Nombre**, **Situación**
  y **Tipo Alumno**.
- No agrega filtros ni exportación porque la pantalla legada no los tenía.
- Usa la tabla compartida, búsqueda, paginación y el estado vacío de SADInformes.
- El botón **Actualizar** vuelve a consultar el informe.

## Backend

| Elemento | Valor |
|---|---|
| Endpoint | `GET /api/reports/academic-enrollment/students-without-priority/report` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Respuesta | Envelope `{ success, message, data }` con `rows` y `total` |
| Origen | `spRec_AlumnosSinPrioridad` |

El procedimiento entrega `Cod_Alumno`, `Nombre`, `Cod_SitAcademica` y
`Tipo_Alumno`; la consulta los expone como `studentCode`, `name`,
`academicStatus` y `studentType`. No se modificó el procedimiento ni el esquema.

La lectura usa `Cache-Control: no-store`. Los errores de SQL, timeout o
configuración se responden como `503` sin exponer detalles internos.

## Verificación

- Pruebas de aplicación y endpoint: mapeo de las cuatro columnas, permiso del
  módulo, rechazo de permisos ajenos, `401` sin sesión y `no-store`.
- Pruebas Angular: carga automática, columnas, búsqueda, estado vacío y error
  sanitizado.

No se verificó contra SQL Server ni en navegador real.
