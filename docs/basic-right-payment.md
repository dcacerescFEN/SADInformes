# Pago Derecho Básico Anual

`/postgrado/pago-derecho-basico-anual` reemplaza `PagoDerechoBasicoAnual.aspx`.
Consulta alumnos de Postgrado, muestra el monto del año y permite registrar el pago
anual de derecho básico.

## Uso

1. Elegir **Tipo Alumno**, **Modalidad**, **Año Ingreso**, **Año del Pago** y
   **Situación Pago**.
2. Pulsar **Mostrar**. La consulta no se ejecuta automáticamente.
3. Marcar o desmarcar **Pago** por alumno, o usar **Marcar todos los pagos**.
4. Pulsar **Guardar** y confirmar. Solo se envían las filas modificadas.
5. Exportar la tabla con **Exportar** si se necesita un XLSX.

El **Código Alumno** enlaza a SADAlumnos usando `studentsMenuUrl` de AppConfig:
`{studentsMenuUrl}/students/detail/{Cod_Alumno}`, la misma ruta que usan Ranking
notas ponderado e Inscripción encuesta salida. El enlace se abre en una pestaña
nueva con `noopener noreferrer`.

La columna Foto usa la URL base `StudentPhotoBaseUrl` de AppConfig, normalizada y
validada como HTTPS institucional, más `{Cod_Persona}.jpg`, el mismo identificador
que usa SADAlumnos; si no hay `Cod_Persona` usa `perfilNN.jpg`. El valor por defecto
es `https://fotos.fen.uchile.cl/`. La foto es solo presentación; no se almacena en
SADInformes.

## API

| Ruta base: `/api/reports/postgraduate/basic-right-payment` | Resultado |
| --- | --- |
| `GET /filters?studentType=MBA` | tipos, modalidades, años y situaciones |
| `GET /report?studentType=&modality=&entryYear=&paymentYear=&paymentStatus=` | monto, filas y `canManage` |
| `GET /download?...` | XLSX de 7 columnas |
| `PUT /payments` | `{ paymentYear, rows: [{ studentCode, paid }] }` |
| Permiso de lectura | `REPORT_VIEW_POSTGRADUATE_BASIC_RIGHT_PAYMENT` |
| Permiso de gestión | `REPORT_MANAGE_POSTGRADUATE_BASIC_RIGHT_PAYMENT` |

La escritura exige ambos permisos y usa la identidad del servidor, no un usuario
enviado por el cliente. El lote se guarda en una transacción Serializable mediante
`spCre_AlumnosXPago_Derecho_Basico`.

## Reglas y defectos legados documentados

- El monto se lee de `PagoDerechoBasicoAnual_Montos`. Si no existe para el año, la
  pantalla muestra **Sin Monto** y bloquea el guardado.
- El legado guardaba todas las filas y actualizaba fechas de auditoría incluso sin
  cambios. La versión nueva envía solo filas modificadas.
- El catálogo nuevo excluye `Todos` en tipo/modalidad porque el SP compara valores
  exactos; el valor antiguo producía una consulta sin resultados.
- El archivo legado se llamaba `Ranking_Programas_Graduados.xls`; el nuevo usa
  `PagoDerechoBasicoAnual.xlsx`.
- No se modifican SPs ni esquema.

## Verificación y límites

Pruebas .NET cubren filtros dependientes, monto, mapeo, foto, guardado transaccional,
permisos y XLSX. Angular cubre filtros, Mostrar, tabla compartida, selección, guardado
confirmado, `Sin Monto`, permiso de gestión y exportación.

No sustituyen SQL Server, fotos reales, Keycloak ni una prueba en navegador. No se
crean permisos en Keycloak ni se publican cambios.
