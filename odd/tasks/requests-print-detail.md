# Solicitudes · Imprimir detalle con lista lazy

Feature: migrar a SADInformes la impresión de detalle de solicitudes usando SSRS y el RDL `SolicitudImpresion`, con una vista de dos paneles: reporte de la solicitud seleccionada a la izquierda y lista de solicitudes a la derecha.

## Specs

S1. Copiar la implementación SSRS de SADAlumnos y usar el mismo RDL `SolicitudImpresion` para imprimir el detalle de una solicitud.

S2. La pantalla de impresión no debe mostrar solamente una solicitud: debe mostrar la lista de solicitudes imprimibles.

S3. La carga debe ser lazy: se obtiene primero la primera solicitud y su reporte; a la derecha se muestra la lista de solicitudes; al seleccionar otra solicitud se carga solamente el reporte de esa solicitud, sin traer todos los reportes al mismo tiempo.

S4. El acceso al detalle debe respetar los permisos existentes de solicitudes e impresión.

S5. Si no hay solicitudes, la pantalla debe informarlo sin pedir un reporte.

## Tasks

| ID | Specs | Route | Estado |
| --- | --- | --- | --- |
| T1 | S1, S4 | Backend SSRS + endpoint + tests | done |
| T2 | S1, S3 | Visor PDF reutilizable | done |
| T3 | S2, S3, S5 | Página de dos paneles, ruta y botón + tests | done |
| T4 | S1-S5 | Documentación y verificación integral | done |

## Log

L1 (usuario): "para la implementacion en sadinformes copia la implementacion de ssr y ocupa el mismo rdl que ocupa sadadlumnos en solicitudes que es SolicitudImpresion pero hay que hacer una modificacion para que cuando imprima no muestre solamente uno sino la lista de impresiones pero hay que hacerla lazy se obtendra la primera solicitud y al a derecha tendra una lista con las solicitudes cuandi el usuario seleccione una solicitud asi no se trae todas al mismo tiempo"

L2 (implementación): backend `SolicitudImpresion` con `CodSolicitud` PDF y endpoint `GET /print-detail`; frontend con visor PDF y página de dos paneles; la lista se carga una vez y solo se pide el PDF de la fila seleccionada.

L3 (verificación): backend 638/638; Angular 449/449; build Angular correcto.

L4 (corrección): el código de solicitud puede venir con `/` y letras desde la base; el backend quita `/`, acepta alfanuméricos y usa el código normalizado en el RDL y en el nombre del PDF. Ejemplo `00773/20262EEYA` -> `0077320262EEYA`.

L5 (UI): botón Imprimir detalle en azul primario para diferenciarlo de Confirmar impresión; lista derecha sin fecha, con buscador y cursor pointer en las filas.
