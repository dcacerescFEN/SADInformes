# Electivos de mención sin plan común

Migración de `SAD/Informes/Inscripcion/ElectivosMencionSinPlanComun.aspx` a la
ruta `/academic-enrollment/electives-without-common-plan`.

## Comportamiento

- Carga el informe automáticamente al abrir la ruta.
- Muestra el título y el texto explicativo legados.
- Presenta las tres columnas originales: **Decreto**, **Código** y **Nombre**.
- No agrega filtros ni exportación porque la pantalla legada no los tenía.
- Usa la tabla compartida, búsqueda, paginación y el estado vacío de SADInformes.
- El botón **Actualizar** vuelve a consultar el informe.

## Backend

| Elemento | Valor |
|---|---|
| Endpoint | `GET /api/reports/academic-enrollment/electives-without-common-plan/report` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Respuesta | Envelope `{ success, message, data }` con `rows` y `total` |
| Origen | `spRec_ElectivosDeMencionSinPlanComun` |

El procedimiento entrega `Decreto2`, `Cod_Catedra` y `Nom_Catedra`; la consulta
los expone como `decree`, `courseCode` y `courseName`. No se modificó el
procedimiento ni el esquema.

La lectura usa `Cache-Control: no-store`. Los errores de SQL, timeout o
configuración se responden como `503` sin exponer detalles internos.

## Verificación

- Pruebas de aplicación y endpoint: mapeo de las tres columnas, permiso del
  módulo, rechazo de permisos ajenos, `401` sin sesión y `no-store`.
- Pruebas Angular: carga automática, columnas, búsqueda, estado vacío y error
  sanitizado.

No se verificó contra SQL Server ni en navegador real.
