# Actualización datos SAD–VTI

La ruta `/postgrado/actualizacion-datos-sad-vti` reemplaza
`ActulizacionDatosSADVTI.aspx`: filtra auditoría por estado y fechas y descarga
**AudiPostgradoAlumnosGuia.xlsx**. No ejecuta sincronizaciones ni modifica datos.
No usa SSRS ni necesita credenciales/configuración de reportes.

## Uso

1. Elegir estado (`Todos` tiene valor `-1`).
2. Ingresar **Desde** y **Hasta**. Ambas fechas son obligatorias y ambos días se
   incluyen completos, incluso registros al final del último día.
3. Pulsar **Descargar Excel**. Una selección sin filas muestra
   **No hay registros para exportar.**, sin descargar un archivo vacío.

El botón evita descargas duplicadas. Cambiar filtros cancela descargas pendientes.
La pantalla mantiene el flujo de exportación original: no agrega tabla ni visor.

## API y permisos

| Recurso | Resultado |
| --- | --- |
| `GET /api/reports/postgraduate/sad-vti-updates/filters` | `{ success, message, data: { states: [{ value, text }] } }` |
| `GET /api/reports/postgraduate/sad-vti-updates/download?state=-1&from=2026-10-01&to=2026-10-31` | Excel OOXML real, no HTML/CSV con extensión falsa |
| Permiso | `REPORT_VIEW_POSTGRADUATE_SAD_VTI_UPDATES` y administradores existentes |
| Fechas API | `yyyy-MM-dd`, sin depender de la cultura del servidor |
| Errores | 400 fechas/estado inválidos o sin filas; 401 sin sesión; 403 sin permiso; 503 almacenamiento/exportación indisponible |

Fechas ausentes, inválidas o invertidas se rechazan antes de consultar SAD.
Estado concreto debe existir en el catálogo. `-1` no agrega predicado de estado;
si se omite, equivale a `-1`. Autorización se aplica a ambos endpoints antes de
consultar datos. Los archivos llevan `Cache-Control: no-store` y `nosniff`.

## Paridad de consulta

Application usa LINQ `AsNoTracking` sobre `ISadContext`. Las definiciones mínimas
provienen de `MappingDB/Models`, sin modificar tablas ni procedimientos:

- `Audi_PostgradoAlumnosGuia` se une al catálogo
  `Audi_EstadoPostgradoAlumnosGuia` por estado.
- Se utiliza el código nuevo si no es nulo; en otro caso, el antiguo. Una cadena
  nueva vacía **no** se sustituye por el código antiguo.
- La unión con `Alumnos.Cod_Alumno` conserva `COLLATE Modern_Spanish_CI_AS`.
- `Personas` se une por el código de persona del alumno, no el de la auditoría.
- Son uniones internas: filas sin estado, alumno o persona coincidentes no salen.
- Se compara la fecha calendario; no se corta el día final por precisión horaria.
- Un apellido final nulo conserva el nombre completo nulo de la concatenación SQL.
- Orden estable por fecha de registro e identificador de auditoría.

Las 12 columnas se conservan, en orden:
`Estado`, `Descripcion`, `Cod_Persona`, `Cod_Alumno_Anterior`, `Cod_Alumno`,
`NombreCompleto`, `Tipo_Alumno`, `Modalidad`, `Cod_SitAcademica`, `Matricula`,
`Fecha_Registro`, `Mensaje_Error`.

El XLSX se genera con `ZipArchive` y XML, sin paquete Excel adicional. Todas las
celdas son texto explícito: códigos conservan ceros iniciales y mensajes que
comienzan por `=`, `+`, `-` o `@` no son fórmulas. Nulos se exportan vacíos.
Fechas se representan como texto `yyyy-MM-dd HH:mm:ss.fff`.
Application agrega referencia directa a EF Core Relational 10.0.12 para
`EF.Functions.Collate`, versión ya transitiva desde Infrastructure.

## Verificación y límites

Pruebas de consulta usan EF InMemory aislado; un interceptor **solo de pruebas**
retira la función de collation no disponible en ese proveedor. Una prueba
separada con el proveedor SQL Server verifica SQL traducido (COLLATE, COALESCE,
CONVERT(date), joins), sin conectarse a la base institucional.

Pruebas HTTP revisan autorización, validación antes de lecturas, catálogos,
selección de estado, nulos, vacío sin adjunto, mensajes seguros y partes XML/ZIP
del XLSX con valores y columnas exactos. Angular cubre filtros, cancelación,
descarga, mensajes y navegación. No reemplazan prueba real de collation/SQL Server,
apertura de Excel o descarga en navegador. No se reinician procesos ni se crean
migraciones o permisos en el proveedor de identidad automáticamente.
