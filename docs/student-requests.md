# Solicitudes alumnos Postgrado

La ruta `/postgrado/solicitudes-alumnos` descarga directamente Excel (xlsx), sin
visor, modal ni vista previa PDF. SSRS sigue ejecutando el reporte
**SolicitudesAlumnoPostgrado**, sin cambiar su nombre ni reconstruir su consulta.

## Uso

1. Elegir tipo de alumno, modalidad, año de ingreso y estado.
2. Pulsar **Descargar Excel**. Se envían los filtros seleccionados y se descarga
   `SolicitudesAlumnoPostgrado.xlsx` sin abrir otra pantalla.
3. Mientras se descarga, no se pueden iniciar descargas duplicadas. Cambiar
   filtros cancela la descarga anterior; respuestas viejas de catálogos no
   reemplazan las nuevas. Los fallos muestran un mensaje seguro y permiten reintentar.

Se conserva `Todos` para tipo/año/estado y modalidad vacía para todas.
No se consulta el reporte al cargar la página, solo sus catálogos.
El estado se envía correctamente: el código heredado enviaba por error el año.

## Backend

| Recurso | Comportamiento |
| --- | --- |
| `GET /api/reports/postgraduate/student-requests/filters?studentType=MBA` | Catálogos en `{ success, message, data }` |
| `GET /api/reports/postgraduate/student-requests` | Descarga Excel (xlsx) predeterminada |
| Parámetros HTTP | `studentType`, `modality`, `cohort`, `state`; `format` opcional solo admite `EXCEL` |
| Formato SSRS | `EXCELOPENXML`; PDF, Word y CSV son rechazados con 400 para este informe |
| Permiso específico | `REPORT_VIEW_POSTGRADUATE_STUDENT_REQUESTS` (roles administradores existentes conservados) |
| Errores | 400 selección inválida, 401 sin sesión, 403 sin permiso, 503 indisponible sin detalles internos |

Application valida catálogos y fija el reporte y los únicos parámetros SSRS permitidos:
`TipoAlumno`, `Modalidad`, `AñoDeIngreso`, `EstadoSolicitud`.
Los valores por defecto se omiten, igual que en el ASPX. El cliente nunca envía
un nombre de reporte, URL, carpeta ni credenciales. Catálogos: `spRec_TiposAlumno`
con `SI`/usuario vacío como el original, `spRec_Años` con `SI`/`Todos` y
`spRec_TiposAlumnosModalidades` por tipo. No hay escrituras académicas.

El transporte replica SADAlumnos: URL codificada, formatos tipados, timeout total
de 60 segundos, streaming, cancelación y liberación de recursos. La configuración
es por aplicación, no estática. El handler no sigue redirects ni usa cookies.
Las respuestas de archivo llevan `Cache-Control: no-store` y `nosniff`.

## Configuración del servidor

Para reemplazar los valores por defecto, provisionar registros activos en AppConfig
para aplicación **SADInformes** y el ambiente seleccionado por `ConfigureSite`:

- `ReportingServiceBaseUrl`: servidor HTTP(S), sin query, fragment ni usuario;
  por defecto `https://reportes-pbi-dev2.fen.uchile.cl/ReportServer`, como SADAlumnos.
- `ReportingServicePath`: carpeta donde existe `SolicitudesAlumnoPostgrado`;
  por defecto `Reportes/Desarrollo/SADInformes`, siguiendo la jerarquía de
  SADAlumnos (`Reportes/Desarrollo/SADAlumnos`) y cambiando solo la aplicación.
- `ReportingServiceDomain`: dominio de autenticación; por defecto, el de SADAlumnos.
- `ReportingServiceUsername`: cuenta de servicio; por defecto, la de SADAlumnos.
- `ReportingServicePassword`: secreto; por defecto, el mismo de SADAlumnos, por
  instrucción explícita del usuario. Su valor no se documenta ni se publica al cliente.

AppConfig tiene prioridad sobre los valores por defecto. Las credenciales ausentes
o nulas usan los defaults; los valores explícitamente vacíos son inválidos. La
contraseña configurada conserva sus espacios. Servidor y carpeta ausentes o nulos
también usan sus defaults. La URL de render por defecto empieza por
`https://reportes-pbi-dev2.fen.uchile.cl/ReportServer?/Reportes/Desarrollo/SADInformes/SolicitudesAlumnoPostgrado`.
Un valor existente de `ReportingServicePath` en AppConfig sigue teniendo prioridad;
si apunta a otra carpeta, debe corregirse allí.
El constructor inicializa esos defaults incluso antes de leer AppConfig. Un valor
explícitamente vacío o una URL/ruta inválida deja el reporte indisponible (503),
sin volver silenciosamente al destino por defecto. Estas claves nunca se incluyen
en la configuración pública. Navegación y otros informes siguen disponibles. El fallback conserva un secreto en el código como en
SADAlumnos; para rotarlo, provisionar AppConfig y retirar después el fallback. La carga ocurre al
arranque: el operador decide el reinicio necesario al provisionar; la migración no
reinicia procesos ni modifica AppConfig institucional.

## Verificación y límites

Pruebas aisladas cubren transporte con HttpMessageHandler falso, endpoints con
renderer/SP falsos, filtros, parámetros, permisos, errores y descarga con HTTP simulado.
Se inventariaron todos los archivos de `SAD/Informes`, no solo Postgrado:
Solicitudes es el único consumidor SSRS real y descarga Excel. Recaudación diaria y
Nómina ayudantes usan Crystal Reports; el registro de ReportViewer en
Actualización SAD–VTI no tiene un control ni render activo. En `SADInformes`, el
visor no tenía otros consumidores y fue eliminado, sin modificar otros sistemas.
No se prueban credenciales, disponibilidad del reporte, permisos del servidor SSRS,
contenido real del Excel ni una descarga desde navegador real.
Esas comprobaciones requieren el entorno y datos autorizados del operador.
