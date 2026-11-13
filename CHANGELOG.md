# Cambios

Todas las versiones de Fortnite Chest Tracker (FCT). Sigue el [versionado semántico](https://semver.org/lang/es/): **MAJOR** cuando algo deja de ser compatible, **MINOR** para funciones nuevas, **PATCH** para arreglos.

## [Sin publicar]

- Idiomas: la app y el informe quedan en español (de España) e inglés. Los demás idiomas se añaden con un archivo JSON por idioma (`src/Localization/Languages/`), que se genera con `dotnet run tools/i18n.cs -- <código>`.
- Compañeros: una sola clasificación con selector (más cofres vistos, mejor media, récord, victorias juntos y la tabla con todos, sin las columnas Récord y Tu media); se quitan la tabla «Por partida» y las tarjetas «Con quién más juegas».
- Textos revisados: tono neutro y español de España («Te damos la bienvenida», «Haz clic», «destruidos»).
- Pantalla de tesoros de fin de partida: FCT captura solo la ventana del juego (aunque esté tapada), reconoce los iconos, borra la imagen y pone nombre a cada artilugio; añade los tesoros que el replay no vio (tras tu eliminación) con hora y jugador desconocidos.
- Requiere Windows 10 2004 o posterior (captura de ventanas de Windows).
- Sin "sesiones": la ventana, el overlay y el informe muestran lo de **hoy** (cofres, partidas y media por partida de hoy).
- Ventana: el gráfico es la semana actual, de lunes a domingo.
- Informe rediseñado en negro y dorado: menú lateral con el logo, cabecera en cada pestaña, tarjetas con icono, gráfico de curva de 30 días, **últimos tesoros** y resumen de compañeros en el Resumen; sin pie de página ni estado "En directo".
- Informe: en un PC cada pestaña cabe en la pantalla; las listas largas se desplazan dentro de su recuadro. En Compañeros, "por partida", "con quién más juegas" y "todos tus compañeros" van en una sección con selector.
- Informe: la caza del tesoro está en Tesoros (premios de la semana como fichas, el resto plegado); textos unificados (mayúscula inicial, versalitas solo en etiquetas) y "Victoria" en vez de "Victoria Real"; se quita la nota larga sobre los cofres de los compañeros.
- Estructura del proyecto: la aplicación en `src/` (`FCT.csproj`) con el código por carpetas (`App`, `Analysis`, `Data`, `Monitoring`, `Overlay`, `Reporting`, `UI`, `Localization`), librería de replays en `external/`, solución `FCT.slnx`.

## [2.0.2] - 2026-11-12

- Sin aviso al volver a leer los replays tras un cambio del análisis.

## [2.0.1] - 2026-11-12

- Los avisos de la ventana se quitan a los 20 s y vuelven los botones Overlay, Informe y Ajustes.

## [2.0.0] - 2026-11-12

Cambio incompatible: los datos pasan a `%APPDATA%\FCT` y la v1.0.0 ya no puede actualizarse sola (hay que descargar `FCT.exe` una vez).

- Al abrir la versión nueva, el historial de la v1.0.0 se traslada solo (si la v1.0.0 está abierta, pide cerrarla).
- Fuera los nombres y el código de versiones anteriores.

## [1.5.0] - 2026-11-12

- Overlay: botón de cerrar a la derecha del candado (cierra la aplicación).

## [1.4.0] - 2026-11-12

- Informe en negro y dorado.
- Tesoros destacados con su nombre y su tipo, sin ids internos; catálogo de premios de la caza del tesoro por semana.
- Nombres de los cofres del juego (Cofre de espíritu, Cofre raro…).

## [1.3.0] - 2026-11-12

- Nuevo nombre: Fortnite Chest Tracker (FCT); el exe pasa a ser `FCT.exe`.
- Actualizar y "Abrir con Fortnite" funcionan aunque el exe tenga otro nombre.

## [1.2.0] - 2026-11-12

- Al abrir solo se ve el overlay; la ventana queda en el área de notificación (flecha ^) y se recupera con un clic o abriendo el programa otra vez.

## [1.1.0] - 2026-11-12

- Partidas: se registra la eliminación definitiva; el informe avisa de que los tesoros del escuadrón posteriores no quedan en el replay.

## [1.0.0] - 2026-11-11

- Primera versión publicada.

[Sin publicar]: https://github.com/adrc4-ua/FChestTracker/compare/v2.0.2...HEAD
[2.0.2]: https://github.com/adrc4-ua/FChestTracker/compare/v2.0.1...v2.0.2
[2.0.1]: https://github.com/adrc4-ua/FChestTracker/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/adrc4-ua/FChestTracker/compare/v1.5.0...v2.0.0
[1.5.0]: https://github.com/adrc4-ua/FChestTracker/compare/v1.4.0...v1.5.0
[1.4.0]: https://github.com/adrc4-ua/FChestTracker/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/adrc4-ua/FChestTracker/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/adrc4-ua/FChestTracker/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/adrc4-ua/FChestTracker/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/adrc4-ua/FChestTracker/releases/tag/v1.0.0
