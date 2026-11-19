# Imprimir detalle de solicitudes

Migración de la acción legacy **Imprimir Detalle Solicitudes** de
`SAD/Informes/Solicitudes/SolicitudesXEstado.aspx`.

## Flujo

Desde la tabla de solicitudes no impresas, el botón **Imprimir detalle** abre
`/requests/print-detail` con los filtros actuales:

```text
career=IC&status=NO IMPRESA&type=67
```

La pantalla carga una sola vez la lista de solicitudes y selecciona la primera.
El panel izquierdo muestra el PDF de esa solicitud; el panel derecho mantiene la
lista con buscador (sin columna de fecha) y filas con cursor pointer. Al seleccionar otra fila se pide únicamente el PDF de esa solicitud. No se
descargan todos los reportes al abrir la pantalla.

## Backend

```text
GET /api/reports/requests/by-status/print-detail?requestCode=<CodSolicitud>
```

- Requiere `REPORT_ACCESS`, `REPORT_VIEW_REQUESTS` y `REPORT_PRINT_REQUESTS`.
- Quita las barras (`/`) que pueda traer el código desde la base de datos y valida que quede alfanumérico de hasta 20 caracteres. Ejemplo: `00773/20262EEYA` se envía como `0077320262EEYA`.
- Genera PDF con el RDL `SolicitudImpresion` y el parámetro `CodSolicitud`.
- Responde `application/pdf`, `Cache-Control: no-store` y `nosniff`.
- Errores: `400` código inválido, `503` reporte no disponible.

## RDL

El reporte se referencia por nombre; no hay `.rdl` versionado en el repositorio.
Para que SADInformes lo encuentre, `SolicitudImpresion` debe estar publicado en
la carpeta configurada en `ReportPath` para SADInformes. El contrato del RDL es
el mismo que usa SADAlumnos: un único parámetro `CodSolicitud`.

## Estados de la pantalla

- Sin solicitudes: muestra **No hay solicitudes para imprimir** y no pide PDF.
- Error de lista: muestra el aviso y no pide PDF.
- Error de PDF: el visor permite reintentar.
- **Volver** regresa a la pantalla anterior.
