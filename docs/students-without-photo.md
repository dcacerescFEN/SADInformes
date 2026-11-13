# Alumnos sin fotos

Migración de `SAD/Informes/Inscripcion/SinFotos.aspx` a la ruta
`/academic-enrollment/students-without-photo`.

## Comportamiento

- No tiene filtros de formulario; consulta al cargar y con **Actualizar**.
- La tabla usa paginación server-side por número de página: cada solicitud recalcula
  desde el inicio, devuelve hasta 10 alumnos confirmados sin foto y no calcula un
  total exacto. Volver atrás o buscar no reutiliza offsets acumulados.
- La búsqueda se resuelve antes de comprobar fotos y reinicia la navegación.
- Usa `spRec_SAD_SAD_PersonasSinFotos`, que entrega alumnos de Ingreso posterior
  a 2008 de tipo `IC`, `IICG` y `CA`.
- Excluye a quienes ya tienen fotografía disponible en la URL institucional.
  La comprobación se hace en backend con HEAD, concurrencia limitada y timeout de
  2 segundos por solicitud, igual que `Utiles.ExisteURL` del legado.
- Solo un `200 OK` confirma que la foto existe. Cualquier otro estado, timeout o
  error de red se considera foto faltante.
- El cliente HTTP no sigue redirecciones. El servidor de fotos responde `301`
  hacia `perfilNN.jpg` cuando la foto no existe; seguirla convertiría la
  respuesta en `200` y ocultaría a todos los alumnos.
- Se conserva la exportación XLSX como `SinFotos.xlsx`.

## Columnas

**Nombre**, **Rut**, **Cód. Alumno**, **Tipo Alumno**, **Celular**,
**Mail Fen**, **Mail Personal**.

## Correo masivo

- Botón **Enviar correo** con confirmación y vista previa de la plantilla legacy.
- El envío corre en segundo plano; la pantalla no espera a que terminen todos los correos.
- El progreso muestra primero **Cargando lista de alumnos** y después **Enviando correo**.
- El modal permite **Minimizar** y **Cancelar envío**; la cancelación no revierte los ya enviados.
- La confirmación no muestra la plantilla: solo explica que primero se carga la lista y luego se envían los correos.
- Usa el mismo permiso del informe: `REPORT_ACCESS_REPORT_MODULE`.
- Configuración SMTP con formato SADAlumnos desde AppConfig:
  `SendEmail`, `EmailFrom`, `EmailTo`, `EmailServer`, `EmailPort`.
- No hay tabla de campañas. El estado del job vive en memoria y se pierde al
  reiniciar el API.
- El progreso se muestra en el centro global de tareas, que se puede minimizar y
  mantener al navegar a otro informe.
- La plantilla conserva el texto de `Button1_Click` y no admite HTML libre.

## Backend

| Elemento | Valor |
|---|---|
| Informe | `GET .../report?page=1&pageSize=10&search=...` |
| Exportación | `GET .../export` |
| Correo: preview | `GET .../email/preview` |
| Correo: inicio | `POST .../email` → `202` |
| Correo: estado | `GET .../email/{jobId}` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Informe | `spRec_SAD_SAD_PersonasSinFotos` |
| Fotos | `StudentPhotoBaseUrl` de AppConfig |

La página se detiene solo al llenar 10 filas o al agotar los candidatos; no hay un
tope de 500 que truncara páginas siguientes. Los resultados de la sonda se cachean
2 minutos por URL para no repetir HEAD al navegar. La respuesta incluye `rows`, `page`, `hasMore` y `scanned`; no incluye total exacto. La exportación sigue recorriendo el informe completo como operación
explícita y usa el `TextWorkbook` compartido (XLSX, `no-store`, `nosniff`).

## Fuera de alcance

- No hay historial persistente ni tabla de campañas: el job de correo vive en
  memoria y se pierde al reiniciar el API.
- El SP no se modificó. No se verificó contra SQL Server, el servidor de fotos ni
  un navegador real.
