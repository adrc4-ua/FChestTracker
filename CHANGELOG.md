# Cambios

Todas las versiones de Fortnite Chest Tracker (FCT). Sigue el [versionado semántico](https://semver.org/lang/es/): **MAJOR** cuando algo deja de ser compatible, **MINOR** para funciones nuevas, **PATCH** para arreglos.

## [Sin publicar]

- **Partidas de antes** (primer uso y Configuración → Cofres antes de FCT → Estimar…): *solo repeticiones guardadas* o *repeticiones + estimación*. Estimado = media × (partidas de la temporada actual − partidas que FCT ya cuenta). Partidas de la temporada buscadas en fortnite-api.com (si la versión lleva la clave) o escritas a mano desde fortnite.gg; aviso si las estadísticas son privadas. Media de tus repeticiones junto con las partidas nuevas que elijas jugar (5 por defecto, 10 recomendado; cada repetición pesa como una partida), o escrita a mano; redondeada hacia abajo solo sin repeticiones o con 100 o más.
- El archivo de la página pasa a llamarse `estadisticas.html` (el antiguo `report.html` se borra).
- Cifras: «tuyos por partida» y «del equipo por partida» de Compañeros dicen ahora «en equipo» (solo cuentan partidas con compañeros; la media del Resumen cuenta todas, también las de solo), y la línea del gráfico es la «media por día jugado».
- `build.bat`: al terminar de compilar `dist\FCT.exe`, abre directamente la aplicación en lugar de abrir la carpeta en el explorador.
- Resumen: alineados los encabezados y los bordes superior e inferior entre «Últimas partidas» y la columna derecha («Últimos tesoros» y el resumen de compañeros).
- Idioma bidireccional: cambiar el idioma en la app regenera y recarga automáticamente la página de Estadísticas abierta con el nuevo idioma, y cambiarlo desde la página (con el bloque de idioma centrado y estilizado como el resto de ajustes) actualiza y reinicia la app automáticamente.
- Tesoros: en «Todas las semanas», se quita la ficha con el recuento total de artilugios.
- La página de estadísticas (antes «Informe» / «Resumen») pasa a llamarse **Estadísticas** en toda la aplicación, la consola y la documentación, y ahora se sirve en `http://localhost:7373/estadisticas` (`http://localhost:7373/estadisticas#resumen`).

## [3.4.1] - 2026-11-12

- Actualizaciones: el aviso de descarga, reinicio o error tiene prioridad sobre el botón de actualizar (antes quedaba oculto detrás del botón); los errores se registran también en `errors.log`.
- Actualizador: reintentos breves al sustituir el `.exe` (por bloqueos puntuales del antivirus) y restauración automática del ejecutable anterior si falla el intercambio.

## [3.4.0] - 2026-11-12

- Actualizaciones: se buscan al iniciar y cada 30 minutos mientras FCT está abierto. Cada release incluye también `ChestCounter.exe` (el mismo exe) para que las versiones de antes del cambio de nombre se actualicen.
- Primer uso: si no es la última versión, pregunta si descargarla (si no, las actualizaciones quedan en *Preguntar*); «Abrir con Fortnite» viene activado.
- «Informe» pasa a llamarse **Resumen** y «Ajustes» **Configuración**, en la app y en la página.
- El resumen tiene su propia configuración (idioma sin reiniciar, compañeros, qué cuenta como cofre): solo cambia la página; la ventana y el overlay usan la de la app. «Abrir con Fortnite» y «Actualizaciones» quedan solo en la app.
- Artilugios con su nombre en español cuando la app va en español (Extractor portátil, Localizador de la suerte, Localizador de espíritus, Acelerador de extracción, Fuegos artificiales dorados).
- Resumen: seis últimas partidas (las dos columnas acaban a la vez); «Cofres del equipo abiertos por tus compañeros» en vez de «Aportan tus compañeros»; columnas numéricas centradas en todas las tablas; Configuración rediseñada.

## [3.3.0] - 2026-11-12

- Informe: pestaña **Ajustes** (idioma, abrir con Fortnite, abrir el informe al iniciar, actualizaciones, compañeros, qué contar como cofre, versión y enlaces a GitHub). El informe lo sirve FCT en `http://localhost:7373/informe` para poder cambiarlos desde la página; si FCT se cierra, la página lo avisa.
- La versión ya no aparece en la app (ni en el título): solo en Ajustes del informe.
- Informe más dorado e iconos nuevos (cofre, tesoros, partidas…); en Últimos tesoros, el icono de cada objeto cuando se sabe cuál es.
- Compañeros en una sola página, sin menú: tabla de todos tus compañeros que se ordena por cualquier columna (partidas, victorias, cofres abiertos, media…) y top 5 de más cofres abiertos y de récord. «Cofres vistos» pasa a «cofres abiertos».
- Resumen: los compañeros siempre a la vista, sin desplazar.
- Tesoros: «Caza del tesoro» pasa a «Tesoros disponibles actualmente» y «De dónde vienen los tesoros» a «Dónde consigo mis tesoros»; «Ver todas las semanas» abre las cinco semanas en una fila y ya no se cierra sola.
- Cofres: la tabla por modo de juego muestra los tesoros de cada modo en vez de las victorias.
- Se quita el aviso «tesoros posteriores no visibles» tras una eliminación: la pantalla de fin de partida ya los recupera.

## [3.2.0] - 2026-11-12

- Idiomas: la app y el informe quedan en español (de España) e inglés. Los demás idiomas se añaden con un archivo JSON por idioma (`src/Localization/Languages/`), que se genera con `dotnet run tools/i18n.cs -- <código>`.
- Compañeros: clasificación unificada con selector (más cofres vistos, mejor media, récord, victorias juntos y la tabla con todos).
- Textos revisados: tono neutro y español de España («Te damos la bienvenida», «Haz clic», «destruidos»).

## [3.1.0] - 2026-11-12

- Informe rediseñado en negro y dorado: menú lateral con el logo, cabecera en cada pestaña, tarjetas con icono, gráfico de curva de 30 días, **últimos tesoros** y resumen de compañeros en el Resumen; sin pie de página ni estado «En directo».
- Informe: en un PC cada pestaña cabe en la pantalla; las listas largas se desplazan dentro de su recuadro. En Compañeros, «por partida», «con quién más juegas» y «todos tus compañeros» van en una sección con selector.
- Informe: la caza del tesoro está en Tesoros (premios de la semana como fichas, el resto plegado); textos unificados (mayúscula inicial, versalitas solo en etiquetas) y «Victoria» en vez de «Victoria Real»; se quita la nota larga sobre los cofres de los compañeros.

## [3.0.0] - 2026-11-12

Cambio incompatible: requiere Windows 10 2004 o posterior (captura de ventanas de Windows).

- Pantalla de tesoros de fin de partida: FCT captura solo la ventana del juego (aunque esté tapada), reconoce los iconos, borra la imagen y pone nombre a cada artilugio; añade los tesoros que el replay no vio (tras tu eliminación) con hora y jugador desconocidos.

## [2.2.0] - 2026-11-12

- Sin «sesiones»: la ventana, el overlay y el informe muestran lo de **hoy** (cofres, partidas y media por partida de hoy).
- Ventana: el gráfico es la semana actual, de lunes a domingo.
- Informe: logo de la app, pestañas más grandes con iconos, estado «En directo», textos unificados (mayúscula inicial) y «Victoria» en vez de «Victoria Real».
- Estructura del proyecto: la aplicación en `src/` (`FCT.csproj`) con el código por carpetas (`App`, `Analysis`, `Data`, `Monitoring`, `Overlay`, `Reporting`, `UI`, `Localization`), librería de replays en `external/`, solución `FCT.slnx`.

## [2.1.0] - 2026-11-12

- Informe: sin pie de página; la caza del tesoro pasa al Resumen (premios de la semana como fichas y el resto plegado en «Ver todas las semanas»).
- Resumen: 5 últimas partidas en vez de 6 y gráfico de 30 días ajustado en móvil.
- Workflow: subir una etiqueta solo compila `FCT.exe`; las releases se publican a mano en GitHub y se puede adjuntar `FCT.exe` a cualquier etiqueta desde Actions.

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

[Sin publicar]: https://github.com/adrc4-ua/FChestTracker/compare/v3.4.1...HEAD
[3.4.1]: https://github.com/adrc4-ua/FChestTracker/compare/v3.4.0...v3.4.1
[3.4.0]: https://github.com/adrc4-ua/FChestTracker/compare/v3.3.0...v3.4.0
[3.3.0]: https://github.com/adrc4-ua/FChestTracker/compare/v3.2.0...v3.3.0
[3.2.0]: https://github.com/adrc4-ua/FChestTracker/compare/v3.1.0...v3.2.0
[3.1.0]: https://github.com/adrc4-ua/FChestTracker/compare/v3.0.0...v3.1.0
[3.0.0]: https://github.com/adrc4-ua/FChestTracker/compare/v2.2.0...v3.0.0
[2.2.0]: https://github.com/adrc4-ua/FChestTracker/compare/v2.1.0...v2.2.0
[2.1.0]: https://github.com/adrc4-ua/FChestTracker/compare/v2.0.2...v2.1.0
[2.0.2]: https://github.com/adrc4-ua/FChestTracker/compare/v2.0.1...v2.0.2
[2.0.1]: https://github.com/adrc4-ua/FChestTracker/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/adrc4-ua/FChestTracker/compare/v1.5.0...v2.0.0
[1.5.0]: https://github.com/adrc4-ua/FChestTracker/compare/v1.4.0...v1.5.0
[1.4.0]: https://github.com/adrc4-ua/FChestTracker/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/adrc4-ua/FChestTracker/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/adrc4-ua/FChestTracker/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/adrc4-ua/FChestTracker/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/adrc4-ua/FChestTracker/releases/tag/v1.0.0
