# Plan: tabla server-side para informes

Estado: Opción A implementada para `students-without-photo`.

## Hallazgo

La `dynamic-table` de SADAdministracion no es server-side: recibe `data`, filtra
y pagina en memoria. La tabla normal de SADAdministracion y SADAlumnos funciona
igual. No existe un componente remoto reutilizable en esos repositorios.

El problema de **Alumnos sin fotos** no es solo el render: la API consulta el SP
completo y luego comprueba la URL de foto de todos los alumnos antes de responder.
Paginar solo en Angular no evita ese costo.

## Objetivo

Crear en SADInformes una `dynamic-table` que:

- Mantenga la apariencia, columnas, skeleton, búsqueda, orden y paginación de
  `app-table`.
- Funcione en modo cliente (comportamiento actual) y en modo servidor.
- En modo servidor reciba solo la página actual y emita `pageChange`,
  `searchChange` y `sortChange`.
- No duplique estilos ni estructura de la tabla normal.

## Contrato propuesto

```ts
mode = input<'client' | 'server'>('client');
data = input<readonly object[]>([]);
total = input<number | null>(null);      // null = total desconocido
page = input<number>(1);
pageSize = input<number>(10);
loading = input<boolean>(false);
pageChange = output<number>();
searchChange = output<string>();
sortChange = output<{ key: string; direction: 'asc' | 'desc' }>();
```

En modo servidor no se filtra, ordena ni corta `data`; solo se renderiza. La
paginación usa `total` si existe; si es `null`, usa anterior/siguiente y un
resumen sin total exacto.

## Problema del total en Alumnos sin fotos

Para saber cuántos alumnos no tienen foto hay que consultar todas las URLs. No se
puede obtener un total exacto barato sin cambiar el SP o mantener un caché.

### Opción A: paginación incremental (recomendada para latencia)

- Endpoint recibe `page`, `pageSize`, `search`.
- Escanea candidatos desde el inicio, salta los confirmados de páginas anteriores y
  devuelve hasta `pageSize` alumnos confirmados sin foto.
- Devuelve `page`, `hasMore`, `scanned`, `total: null`.
- Primera carga rápida; sin número total ni salto directo a una página lejana.
- Exportación sigue siendo una operación explícita y potencialmente lenta.

### Opción B: total exacto con caché

- Primera carga escanea todo y guarda el resultado por unos minutos.
- Mantiene paginación numérica y total exacto.
- La primera carga sigue siendo lenta; las siguientes son rápidas.
- Requiere caché, expiración y coordinación entre instancias.

### Opción C: híbrida

- Devuelve la primera página rápido y calcula el total en segundo plano.
- El frontend muestra el total cuando está disponible.
- Mayor complejidad operativa y de pruebas.

## Implementación actual

- `app-dynamic-table` envuelve `app-table` y conserva su estructura, estilos,
  skeleton, búsqueda externa y paginación.
- `app-table` acepta `mode`, `total`, `page`, `pageSize`, `hasMore`, `pageChange` y
  `sortChange`. En modo servidor no filtra, ordena ni corta datos localmente.
- `students-without-photo` usa número de página estable, sin historial de offsets
  ni total exacto; cada página se recalcula desde el inicio.
- La búsqueda server-side se aplica a los candidatos antes de la sonda de fotos.

## Recomendación

Opción A para `students-without-photo`: resuelve la queja de latencia y evita
fingir un total que no se conoce. La `dynamic-table` debe soportar `total: null`
y usar navegación anterior/siguiente en ese caso.

## Fases

1. [completado] Crear `dynamic-table` con modos cliente y servidor, reutilizando
   los estilos y la estructura de `app-table`.
2. [completado] Agregar el modo servidor al endpoint de Alumnos sin fotos:
   `page`, `pageSize`, `search`; respuesta con `rows`, `page`, `hasMore`, `scanned`.
3. [completado] Migrar la pantalla a `dynamic-table` server-side.
4. [completado] Mantener exportación como caso de uso separado y documentar su costo.
5. [pendiente] Evaluar el modo server-side para otros informes con volumen alto.

## Pruebas

- Modo cliente conserva filtrado, orden y paginación actuales.
- Modo servidor no corta ni filtra localmente.
- `pageChange` no dispara dos solicitudes.
- `total: null` muestra navegación sin total y deshabilita saltos inexistentes.
- Búsqueda server-side reinicia a la primera página.
- Cambio de filtros invalida respuestas viejas.
- Exportación y tabla usan la misma definición de columnas.

## Pendientes

- Evaluar otros informes de volumen alto.
- Revisar el límite de 500 candidatos escaneados por solicitud con datos reales.
