# Actividades Cursos Postgrado

`/postgraduate/course-activities` reemplaza `ActividadesCursosPostgrado.aspx`.
Consulta las actividades de cursos de Postgrado por año y permite buscar y exportar
las 22 columnas del legado.

## Uso

1. Elegir **Año**. Al entrar se carga el primer año disponible.
2. Pulsar **Mostrar** para recargar el año seleccionado.
3. Filtrar la tabla con el buscador compartido.
4. Pulsar **Exportar** para descargar
   `Reporte Actividades Cursos Postgrado - {año}.xlsx`.

Columnas: Id, Tipo, Catedra, Desde, Hasta, Periodo, Cod_Catedra, Cod_Seccion,
Programa, Modalidad, Promocion, Sala, Capacidad_Sala, Estudiantes_Inscritos,
Tipo_Sala, Es_Sala_Hibrida, Fecha_Ingreso, UsuarioIngreso, Suspendida,
Fecha_Suspención, Usuario_Suspención y Observaciones.

## API

| Ruta base: `/api/reports/postgraduate/course-activities` | Resultado |
| --- | --- |
| `GET /filters` | años disponibles |
| `GET /report?year=2024` | filas y total |
| `GET /download?year=2024` | XLSX de 22 columnas |
| Permiso | `REPORT_VIEW_POSTGRADUATE` |
| Errores | 400 año inválido o exportación vacía; 401/403; 503 sanitizado |

Las respuestas usan `Cache-Control: no-store`; el archivo lleva
`X-Content-Type-Options: nosniff`.

## Contrato

Los años salen de `spRec_Años('NO','Todos')` y el reporte de
`spRec_ReporteActividadesCursosPostgrado(@Anno int)`. El SP ya no usa el año actual
fijo: recibe el año seleccionado y conserva la lógica de incluir el año siguiente
después de agosto.

## Defectos legados documentados

- El botón Mostrar legado no tenía manejador explícito; el nuevo botón recarga el
  año seleccionado de forma determinista.
- La advertencia por capacidad del SP se activa desde `capacidad - 5`, por lo que
  puede marcar salas antes de estar llenas. No se modificó el SP.
- `Fecha_Suspención` se formatea a `dd-MM-yyyy HH:mm:ss`, igual que las demás fechas
  del reporte.
- El legado no validaba un permiso específico en el code-behind; la pantalla nueva
  exige el permiso de backend indicado arriba.

## Verificación y límites

Pruebas Application cubren años, año por defecto y las 22 columnas. HTTP cubre
permiso, nombre/columnas del XLSX y el caso sin datos. Angular cubre la carga
automática, recarga por año, tabla compartida, búsqueda, exportación, `Sin Datos`
y errores saneados.

No sustituyen SQL Server, Excel ni una prueba en navegador. No se crean permisos en
Keycloak ni se publican cambios.
