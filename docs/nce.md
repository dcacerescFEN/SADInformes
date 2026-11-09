# Informe Notificaciones Causal de Eliminación (NCE)

`/postgraduate/nce` migra **solo el informe** que vive
en Maestro. No incluye el listado administrable, `Bloquear/Activar`, el modal de
email ni el iframe/cookie `AdminPermiso` del SAD legado.

## Uso

La página carga el informe al entrar y permite:
- buscar en la tabla compartida;
- ordenar por columna;
- exportar **Excel** y **CSV**;
- descargar **PDF** generado por el backend.

Columnas: Fila, ID, Fecha, Causal, Código Alumno, Tipo Alumno, Modalidad,
Situación Académica, Motivo y Nombre.

Los botones de exportación usan clases y tokens compartidos: Excel verde
(`--export-excel`) y PDF rojo (`--export-pdf`).

## API

| Ruta base: `/api/reports/postgraduate/nce` | Resultado |
| --- | --- |
| `GET /report` | filas del informe |
| `GET /download` | XLSX del informe |
| `GET /download/pdf` | PDF del informe |
| Permiso | `REPORT_VIEW_POSTGRADUATE` |
| Errores | 400 sin datos al exportar; 401/403; 503 sanitizado |

Las respuestas usan `Cache-Control: no-store`; el archivo lleva
`X-Content-Type-Options: nosniff`.

## Datos

Se usan las tablas `SAD.NotificacionCausalEliminacionPostgrado`,
`SAD.CausalEliminacionPostgrado` y `SAD.TipoCausalEliminacionPostgrado`, con
`Alumnos` y `Personas` para el informe. No se modifica el esquema.

El informe conserva el orden de Maestro: fecha descendente y código de alumno;
mantiene una fila por causal, como Maestro.

## Alcance y seguridad

- Solo lectura: no se exponen endpoints de escritura ni acciones de gestión.
- La gestión de notificaciones sigue en Maestro.
- Se elimina el iframe y la cookie Base64 `AdminPermiso`.
- Maestro permitía `frame-ancestors *` y no tenía `[Authorize]` en su controlador;
  SADInformes exige un permiso de backend para leer el informe.

## Verificación y límites

Pruebas .NET cubren el mapeo del informe, permiso, XLSX y PDF real (cabecera `%PDF`). Angular cubre carga,
tabla compartida, búsqueda, CSV/Excel/PDF, `Sin Datos` y errores saneados.

El PDF usa PDFsharp/MigraDoc 6.2.2 con fuentes Carlito embebidas (SIL OFL), página
Legal horizontal y encabezado repetido. Así el resultado no depende de fuentes del
servidor ni de pdfmake en el navegador.

No sustituyen SQL Server, Keycloak ni una prueba visual en navegador. No se crean
permisos en Keycloak ni se publican cambios.
