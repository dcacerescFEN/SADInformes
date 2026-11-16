# Diferencias entre `variables.scss` y `styles.scss`

## Fuentes revisadas

- `SADAlumnos/src/Web/WebUI/src/variables.scss`
- `SADAlumnos/src/Web/WebUI/src/styles.scss`
- `SADInformes/src/Web/WebUI/src/variables.scss`
- `SADInformes/src/Web/WebUI/src/styles.scss`

`SADInformes/src/Web/WebUI/src/variables.scss` es una copia de la paleta de
SADAlumnos. La diferencia no está en el archivo de variables, sino en cómo
`styles.scss` consume esas variables.

## Situación actual

### SADAlumnos

`styles.scss` importa las variables y las usa para la paleta principal:

```scss
@use "variables" as vars;
```

Se usan como variables Sass:

- `$primary-blue`
- `$primary-blue-deep`
- `$secondary-blue`
- `$light-blue`
- `$success-green`
- `$danger-red`
- `$warning-yellow`
- `$border-gray`
- `$card-shadow`
- `$card-radius`

Pero no todas las variables se exponen como tokens CSS. Siguen hardcodeados:

- `--layout-background`
- `--layout-surface`
- `--layout-surface-strong`
- `--layout-text`
- `--layout-muted-text`
- `--layout-border-color`
- `--input-*`
- `--table-*`
- `--dropdown-*`
- Varios botones con colores como `#198754`, `#dc3545`, `#003087`

Tampoco se exponen directamente:

- `$dark-gray`
- `$muted-gray`
- `$hover-gray`
- `$light-gray`
- `$page-gray`
- `$border-color`
- `$border-color-soft`

### SADInformes

`styles.scss` ya importa las variables:

```scss
@use "variables" as vars;
```

Pero actualmente solo las usa para:

```scss
--warning-yellow
--dark-gray
```

El resto de los tokens sigue hardcodeado y algunos valores difieren de
`variables.scss`.

## Diferencias detectadas

| Variable Sass | Valor en `variables.scss` | Token/valor actual en SADInformes | Estado |
|---|---:|---|---|
| `$primary-blue` | `#003b7a` | `--primary-blue: #003b7a` | Igual, pero hardcodeado |
| `$primary-blue-deep` | `#06245a` | `--primary-blue-deep: #06245a` | Igual, pero hardcodeado |
| `$secondary-blue` | `#2c4a8c` | `--secondary-blue: #2c4a8c` | Igual, pero hardcodeado |
| `$light-blue` | `#e8f1f8` | `--light-blue: #dbe8f8` | **Diferente** |
| `$border-gray` | `#dee2e6` | `--border-gray: #cfd8e3` | **Diferente** |
| `$page-gray` | `#f3f6fb` | `--layout-background: #f4f7fb` | **Diferente** |
| `$hover-gray` | `#f8f9fa` | `--table-row-hover-background: #f3f6fb` | **Diferente** |
| `$card-shadow` | `0 2px 8px rgba(0,0,0,.08)` | `0 2px 10px rgba(15,35,70,.06)` | **Diferente** |
| `$card-radius` | `12px` | `0.75rem` | Equivalente |
| `$success-green` | `#198754` | `--export-excel: #198754` | Mismo color, semántica distinta |
| `$danger-red` | `#dc3545` | `--export-pdf: #b02a37` | **Diferente** |
| `$muted-gray` | `#6c757d` | `--layout-muted-text: #607086` | **Diferente** |
| `$warning-yellow` | `#ffc107` | `var(--warning-yellow)` | Correcto |
| `$dark-gray` | `#343a40` | `var(--dark-gray)` | Correcto |
| `$border-color` | `#e9ecef` | `--layout-border-color: rgba(214,223,235,.9)` | **Diferente** |
| `$border-color-soft` | `#dbe4ef` | Sin token directo | **Falta** |
| `$light-gray` | `#f8f9fa` | Sin token directo | **Falta** |
| `$muted-gray` | `#6c757d` | Sin token directo | **Falta** |

## Observaciones

1. El archivo de variables no es todavía la fuente única de verdad: `styles.scss`
   repite valores que ya existen en la paleta.
2. Varios valores actuales de SADInformes no coinciden con SADAlumnos. Cambiarlos
   afecta visualmente inputs, tablas, bordes, hover, tarjetas y fondo.
3. El resaltado de las columnas `Tot. Ocup.` y `Rec. Vac. Real` ya usa
   `var(--warning-yellow)`, que proviene de `$warning-yellow`.
4. SADAlumnos también tiene hardcodeos en su propio `styles.scss`; no es correcto
   asumir que todo su estilo deriva de `variables.scss`.

## Plan propuesto

### Fase 1 — Definir la fuente de verdad

- Confirmar que `variables.scss` es la referencia de paleta.
- Decidir si los tokens de layout (`--layout-*`, `--input-*`, `--table-*`,
  `--dropdown-*`) también deben vivir en `variables.scss` o mantenerse como
  tokens semánticos de cada aplicación.
- Documentar los tokens que no existen en la paleta: texto, superficies, bordes
  semánticos y estados de tabla.

### Fase 2 — Mapear variables a tokens CSS

Exponer en `:root` los valores que hoy están hardcodeados, usando
`#{vars.$...}` cuando exista una variable equivalente:

- `--primary-blue`
- `--primary-blue-deep`
- `--secondary-blue`
- `--light-blue`
- `--success-green`
- `--danger-red`
- `--warning-yellow`
- `--dark-gray`
- `--muted-gray`
- `--hover-gray`
- `--border-gray`
- `--card-shadow`
- `--card-radius`

### Fase 3 — Decidir valores discrepantes

Antes de reemplazar, definir para cada diferencia si se adopta el valor de
SADAlumnos o se conserva el de SADInformes:

- `--light-blue`
- `--border-gray`
- `--layout-background`
- `--table-row-hover-background`
- `--card-shadow`
- `--layout-muted-text`
- `--export-pdf`

Sin esta decisión, la alineación cambia la apariencia actual.

### Fase 4 — Eliminar hardcodeos duplicados

- Reemplazar colores repetidos por variables Sass en `styles.scss`.
- Mantener tokens semánticos de SADInformes cuando el nombre de SADAlumnos no
  represente el mismo uso.
- No introducir nuevas variables sin documentarlas.

### Fase 5 — Verificación

- Compilar Angular.
- Revisar modo claro y oscuro.
- Revisar tablas, hover, formularios, sidebar y tarjetas.
- Ejecutar pruebas Angular afectadas.
- Documentar cualquier diferencia visual intencional.

## Estado

Documento de análisis y plan. No se modificó `styles.scss` en este paso.
