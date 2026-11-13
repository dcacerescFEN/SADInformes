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

## Backend

| Elemento | Valor |
|---|---|
| Informe | `GET .../report?page=1&pageSize=10&search=...` |
| Exportación | `GET .../export` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Informe | `spRec_SAD_SAD_PersonasSinFotos` |
| Fotos | `StudentPhotoBaseUrl` de AppConfig |

La página se detiene solo al llenar 10 filas o al agotar los candidatos; no hay un
tope de 500 que truncara páginas siguientes. Los resultados de la sonda se cachean
2 minutos por URL para no repetir HEAD al navegar. La respuesta incluye `rows`, `page`, `hasMore` y `scanned`; no incluye total exacto. La exportación sigue recorriendo el informe completo como operación
explícita y usa el `TextWorkbook` compartido (XLSX, `no-store`, `nosniff`).

## Fuera de alcance

- El envío masivo de correo de la pantalla legada no se migró: requiere SMTP y
  una acción explícita, no forma parte de la consulta del informe.
- El SP no se modificó. No se verificó contra SQL Server, el servidor de fotos ni
  un navegador real.
