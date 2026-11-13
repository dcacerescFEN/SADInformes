# Inscripción académica: alumnos sin fotos

Migrar `SAD/Informes/Inscripcion/SinFotos.aspx`.

## Specs

- **S1:** La ruta `/academic-enrollment/students-without-photo` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Consulta `spRec_SAD_SAD_PersonasSinFotos` y excluye a quienes tienen foto disponible, consultando `StudentPhotoBaseUrl`.
- **S3:** Conservar las siete columnas legadas: Nombre, Rut, Cód. Alumno, Tipo Alumno, Celular, Mail Fen y Mail Personal.
- **S4:** Mantener la exportación XLSX como `SinFotos.xlsx`.
- **S5:** Usar `page-header`, `form-section`, `app-table`, skeleton, búsqueda y estado vacío compartidos. El envío de correo queda fuera de alcance.
- **S6:** Paginar y buscar en server-side con `dynamic-table`, sin cargar todas las filas ni calcular un total exacto; exportación completa separada.

## Tasks

- T1 | S1-S3 | backend: resultado, wrapper, sonda de fotos, query y endpoints | completado
- T2 | S1-S2, S4-S5 | frontend: tabla, exportación y ruta | completado
- T3 | S1-S5 | pruebas backend/frontend y documentación | completado
- T4 | S1, S6 | tabla server-side incremental, endpoint offset/pageSize/search y plan de correo | completado

## Log

L1. Solicitud del usuario: «continua con Informes/Inscripcion/SinFotos.aspx».
L2. Legacy: sin filtros; excluía alumnos cuya URL de foto existía y permitía envío masivo de correo.
L3. El envío de correo no se migra: requiere SMTP y una acción explícita.
L4. La sonda usa HEAD y solo un 200 confirma foto existente; cualquier otro estado, timeout o error se considera faltante, como el legado.
L5. Verificación final: backend enfocado 10/10 y suite WebApi 594/594; Angular enfocado 7/7 (pantalla + dynamic-table) y suite completa 396/396; Node 38/38; build Angular correcto. Sin verificación SQL, servidor de fotos ni navegador real.
L8. Corrección tras reporte de tabla vacía: el servidor de fotos responde 301 a perfilNN.jpg para fotos faltantes. HttpClient seguía la redirección y veía 200; ahora AllowAutoRedirect=false y solo 200 directo cuenta como foto existente.
L9. Corrección de navegación: se reemplazó el historial de offsets por número de página recalculado desde el inicio, para que Anterior y la búsqueda devuelvan páginas estables. Se eliminó el tope de 500 candidatos que truncaba la segunda página y se agregó caché de 2 minutos por URL de foto.
L6. Se implementó `dynamic-table` server-side con `offset`/`nextOffset`, sin total exacto, y búsqueda antes de la sonda. Planes en `docs/server-side-dynamic-table-plan.md` y `docs/bulk-email-plan.md`.
L7. Verificación independiente previa a la paginación: PASS en permiso/PII, sonda, orden, XLSX y frontend estructural. Su único blocker era la eliminación del enlace de Postgrado, que fue una instrucción explícita del usuario en el turno anterior y se mantiene. Sus pruebas adicionales de sonda, orden, concurrencia, 403 y exportación vacía se incorporaron a `StudentsWithoutPhotoTests`.
