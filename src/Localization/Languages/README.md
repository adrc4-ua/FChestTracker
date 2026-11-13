# Idiomas

FCT está en **español** e **inglés**. Los dos se escriben directamente en el código:

```csharp
L.T("Cofres abiertos", "Chests opened")
L.F("{0} % de tus partidas", "{0} % of your matches", rate)
L.Count(n, "cofre", "cofres", "chest", "chests")
```

Cualquier otro idioma es **un archivo JSON en esta carpeta**. Va dentro del exe al compilar y aparece solo en
**Ajustes → Idioma**. Lo que no esté traducido sale en inglés.

## Añadir un idioma

1. Crea el archivo con todos los textos (ejemplo: portugués de Brasil):

   ```
   dotnet run tools/i18n.cs -- pt --name "Português (Brasil)" --culture pt-BR
   ```

   Se crea `pt.json` con cada texto: la **clave** es el texto en español y el **valor** empieza en inglés.

2. Traduce los valores. No cambies las claves ni los huecos `{0}`, `{1}`… Los textos largos tienen una clave corta
   que empieza por `#` (por ejemplo `#help`).

3. Compila. El idioma ya sale en Ajustes. Para probarlo sin tocar Ajustes: variable de entorno `FCT_LANG=pt`.

Cuando cambien los textos de la app, vuelve a ejecutar el mismo comando: conserva lo traducido, añade los textos
nuevos (en inglés) y quita los que ya no se usan.

## Formato

```json
{
  "name": "Português (Brasil)",
  "culture": "pt-BR",
  "ordinal": "{0}º",
  "texts": {
    "Cofres abiertos": "Baús abertos"
  }
}
```

| Campo | Para qué |
|---|---|
| `name` | Nombre del idioma en ese mismo idioma, tal cual sale en Ajustes. |
| `culture` | Cultura de .NET para fechas y números (`pt-BR`, `fr-FR`…). |
| `ordinal` | Puesto en la partida: `{0}º`, `{0}e`… (por defecto `{0}.`). |
| `texts` | Texto en español → traducción. |

El código del idioma es el nombre del archivo (`pt.json` → `pt`) y debe coincidir con el idioma de Windows para que
se elija solo.
