# Cambios

Todas las versiones de Fortnite Chest Tracker (FCT). Sigue el [versionado semántico](https://semver.org/lang/es/): **MAJOR** cuando algo deja de ser compatible, **MINOR** para funciones nuevas, **PATCH** para arreglos.

## [Sin publicar]

- Ya no se trae la carpeta de datos de la v1.0.0 (cuando la app se llamaba ChestCounter): esa versión no podía actualizarse sola. Si alguien la tuviera, sus datos siguen en `%APPDATA%\ChestCounter`.
- **Licencia: GNU GPL versión 3** (antes MIT; las versiones hasta la 4.4.1 siguen siendo MIT), con condiciones de atribución: quien distribuya FCT o algo basado en él debe publicar su código con la misma licencia, mencionar al autor y este repositorio y marcar sus cambios ([NOTICE.md](NOTICE.md)). Nuevo **Acerca de y licencia** en el menú del icono. ChestCounter aparece como lo que es, el proyecto del que FCT es un fork, y conserva su aviso MIT.
- **Desinstalar** desde Configuración (o `FCT.exe --uninstall`): FCT se cierra y borra el exe, la tarea programada de «Abrir con Fortnite», las entradas de inicio y los restos de actualizaciones, sin tener que desactivar nada antes. Tus datos se quedan en `%APPDATA%\FCT`.
- **Avisos legales**: nuevo [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) con todo lo externo (FortniteReplayDecompressor, ChestCounter, .NET, Fortnite-API, fortnite.gg, SignPath) y sus licencias o condiciones; también va dentro del exe (`FCT.exe --licenses`). LICENSE, README y la política de firma de código lo detallan, con el aviso de marcas de Epic Games, cómo desinstalar y los metadatos del exe (producto, versión, copyright). Las notas de cada release enlazan la política de firma.
- **Configuración más limpia**: sin «Atajos del overlay» ni el pie de «Código fuente»; las dos columnas acaban a la misma altura, con «Restablecer estadísticas» y «Cerrar» en la misma línea. Actualizaciones explica cada opción y el overlay para OBS cómo usarlo. El idioma, centrado en su botón, con la flecha del desplegable aparte.
- **Abrir con Fortnite sin nada en marcha**: ya no hay un proceso invisible esperando desde el inicio de Windows ni aparece en «Aplicaciones de arranque». Ahora es una tarea programada de tu usuario (sin permisos de administrador) que salta cuando el antitrampas de Fortnite carga su driver, algo que Windows anota en su registro; un lanzador breve espera a que aparezca el juego, abre FCT y termina (con otro juego que use el mismo antitrampas se cierra sin hacer nada). Al abrir FCT se pasa solo al sistema nuevo y borra las entradas de inicio de versiones anteriores (también la de un nombre antiguo, «FortniteChestTracker»). Si la tarea no se pudiera crear, sigue funcionando como antes.
- El repositorio pasa a llamarse **Fortnite-Chest-Tracker** (antes FChestTracker): los enlaces de la app, de la página de estadísticas, de Configuración y de este README apuntan al nombre nuevo. Las copias ya instaladas siguen encontrando las actualizaciones, porque GitHub redirige el nombre antiguo.

## [4.4.1] - 2026-11-13

- Corregido: una repetición muy corta justo después de una partida (entrar y salir) se quedaba también con la pantalla de tesoros de esa partida, y su tesoro se contaba dos veces. Al abrir FCT se corrige solo en los datos guardados.
- **Overlay y tarjetas**: sin la raya que dejaba el velo dorado; ajuste de espaciado entre Hoy, Media y Tesoros, con líneas doradas finas entre las columnas de cifras y bloque de cifras centrado.

## [4.4.0] - 2026-11-12

- **Varios jugadores en un PC**: FCT sigue al que juega (al empezar o terminar una partida de otra cuenta cambia a su perfil y avisa «Ahora juega X»); la partida en directo de uno ya no se suma al overlay ni a las cifras del otro; «Cofres antes de FCT» es de cada jugador (el estimado de versiones anteriores pasa a su perfil); al cambiar de perfil, la página de estadísticas cambia al momento. El historial de cada cuenta se conserva como siempre.
- **Un solo FCT a la vez**, sea cual sea la carpeta de datos (antes, uno por carpeta: podían salir dos overlays). Al abrirlo otra vez no se abre otro: el que ya está saca su ventana con el aviso «FCT ya estaba abierto»; si aún está en la bienvenida, el segundo lo avisa con su propio mensaje. Abierto por Fortnite estando ya abierto, se cierra sin decir nada.
- **Overlay con el estilo de la app** (igual de sencillo): negro con el velo dorado de las tarjetas, borde y franja de oro (menta en directo), «◆ COFRES», cifras grandes con el degradado de oro, cápsula dorada para la partida, columnas centradas y la gema dorada para los tesoros. La versión para OBS, igual.

## [4.3.0] - 2026-11-12

- **Tesoros que la repetición no ve** (conseguidos después de morir): la pantalla de fin de partida los suma siempre, con su nombre si se lee y sin él si no; una tarjeta ilegible ya no hace perder la pantalla entera, y también se suman los tesoros destacados. Cada pantalla se asocia solo a su partida: antes, una partida sin pantalla leída podía quedarse con la de la siguiente.
- **Pantalla de tesoros de fin de partida**: ahora se leen bien las tarjetas cuyo icono las cruza o las llena (Acelerador de extracción, fuegos artificiales) y ya no se confunde el rótulo dorado de «Victoria» con una tarjeta. Comprobado con capturas reales: acelerador, localizador de la suerte + fuegos, localizador de espíritus, fuegos y extractor portátil. Se capturan más veces (durante 9 s) para no llegar antes de que aparezcan todas.
- **Overlay más fiable**: solo cuenta una ventana de Fortnite que esté de verdad en pantalla (con tamaño real, dentro de un monitor y sin ocultar por Windows), así no aparece con Fortnite minimizado; comprueba cada 2 s desde su propio hilo, así se va al cerrar Fortnite aunque FCT esté leyendo repeticiones; y apagado a mano ya no se vuelve a encender al minimizar y restaurar Fortnite (solo al abrir un Fortnite nuevo).
- **Overlay y gráficos nítidos**: textos más nítidos (ningún texto por debajo de 9 px) y a la escala del monitor donde está, también al moverlo a otra pantalla. Gráfico semanal con textos nítidos y la cifra siempre encima de su punto. Ventana: el botón Estadísticas es como los demás, con las letras en oro; las cifras de las seis tarjetas, algo más arriba.
- **Buscar partidas (fortnite-api.com)**: una cuenta sin partidas esta temporada ya no sale como «no encontramos a nadie con ese nombre» (son 0 partidas); mensajes propios para demasiadas búsquedas a la vez (límite de 180 por minuto) y clave no válida. Probado con distintos nombres: mayúsculas, espacios, perfiles privados, nombres inexistentes y caracteres raros.
- Configuración → Recalcular: sin ninguna partida guardada, la primera opción es «Empezar de cero» (como en la primera apertura). Comprobado que desde Configuración las partidas guardadas cuentan igual que en la primera apertura.
- Textos: «Calcular partidas previas» en la primera apertura y el diálogo; «Recalcular» en Configuración. Estadísticas vuelve a ir en blanco, como los otros botones.
- **Firma del ejecutable preparada** (SignPath, gratis para código abierto): [política de firma](CODE_SIGNING_POLICY.md) y paso de firma en el flujo de GitHub, que se activa en cuanto SignPath esté configurado. Hasta entonces Windows puede avisar al abrir el exe (README: *Más información → Ejecutar de todas formas*).
- Las releases ya no llevan la copia `ChestCounter.exe`: las versiones de antes del cambio de nombre ya no se actualizan solas (hay que descargar `FCT.exe` una vez).
## [4.2.1] - 2026-11-12

- Primer uso: la opción sin estimado pasa a llamarse **Empezar de cero** cuando no se cuentan partidas guardadas (no hay repeticiones o se eligió *Solo las nuevas*).
- Documentación (`README.md`, `README.en.md`) y versión de compilación (`4.2.1`) actualizadas.

## [4.2.0] - 2026-11-12

- **Esta semana** con el gráfico de la página: curva dorada con relleno, escala, media discontinua y puntos (hoy en blanco). Tarjeta principal con las cifras centradas.
- Configuración → Cofres antes de FCT: cifra entre `−` y `+`, y el botón **Recalcular** (abre el mismo diálogo de la primera apertura, «Calcular partidas previas»).
- **Abrir con Fortnite** activado por defecto (también una vez en instalaciones previas al actualizar; luego se respeta lo que elijas). Una copia de prueba (`--data`) ya no cambia el inicio de Windows al activarlo.

## [4.1.0] - 2026-11-12

- **Sin logo en la interfaz** (solo en la barra de título): ventana principal con la estructura clásica (cofres y última partida, sus detalles, la semana; seis cifras en rejilla y los botones) sobre el diseño negro y oro. El perfil pasa a mostrarse como texto.
- **Textos más cercanos y cortos** en la bienvenida, el aviso de versión nueva, «Partidas de antes» y los avisos de Configuración, con iconos con sentido; restablecer estadísticas y otros avisos dejan de usar cuadros grises de Windows.
- **Partidas de antes**: solo se pide el nombre de Epic y se buscan las partidas; `fortnite.gg` y el número a mano solo aparecen si no se encuentran o el perfil es privado. Arreglados los recuadros oscuros de los campos de texto.
- **Por defecto activados**: overlay y compañeros (en ventana y página).

## [4.0.0] - 2026-11-12

Cambio incompatible en la interfaz de usuario: rediseño visual completo de la aplicación de escritorio en negro y oro.

- **Nuevo diseño de la app**, a juego con la página de estadísticas: negro y oro, tarjetas con velo dorado, los mismos iconos SVG y estética unificada en ventana principal, Configuración, bienvenida y diálogos.
- Corregido: al aparecer un aviso o la tarjeta de equipo, la ventana podía mostrar el error `Parameter is not valid` por fuentes liberadas al reescalar.

## [3.7.0] - 2026-11-12

- **Overlay**: solo se ve con Fortnite abierto (sin minimizar) en alguna pantalla; entonces se puede poner en cualquier pantalla y sitio, sin salirse por los bordes al arrastrarlo. Se enciende solo al abrir Fortnite y se oculta al cerrarlo aunque la ventana de FCT siga visible.
- **Abrir y cerrar**: con Fortnite abierto, FCT arranca en segundo plano con el overlay; sin Fortnite, abre la ventana. La `X` de la ventana la deja en segundo plano si Fortnite está abierto (si no, cierra); la `✕` del overlay cierra FCT. En segundo plano se cierra solo al cerrar Fortnite, o desde *Salir* en el área de notificación.

## [3.6.0] - 2026-11-12

- **Partidas de antes** (primer uso y Configuración → Cofres antes de FCT → *Estimar…*): *solo repeticiones guardadas* o *repeticiones + estimación*. Estimado = media × (partidas de la temporada actual − repeticiones de esta temporada que FCT ya cuenta). Partidas de la temporada buscadas en `fortnite-api.com` (si la compilación lleva clave) o escritas a mano desde `fortnite.gg` (con aviso si el perfil es privado); media ponderada entre repeticiones guardadas y partidas nuevas (5 por defecto, 10 recomendado) o manual.
- Ventana principal: los avisos y actualizaciones se muestran en una franja propia bajo la ventana sin tapar los botones; avisa también mientras lee una tanda de replays (`Leyendo tus replays… 3 / 40`).
- Primer uso: cerrar cualquiera de las ventanas iniciales cierra la app sin guardar nada; al volver a abrirla empieza de nuevo.

## [3.5.0] - 2026-11-12

- La página de estadísticas (antes «Informe» / «Resumen») pasa a llamarse **Estadísticas** en toda la aplicación, la consola y la documentación; ahora se sirve en `http://localhost:7373/estadisticas` (`#resumen`) y se guarda como `estadisticas.html` (el antiguo `report.html` se borra).
- **Idioma bidireccional**: cambiar el idioma en la app regenera y recarga automáticamente la página de Estadísticas abierta con el nuevo idioma, y cambiarlo desde la página (con el bloque de idioma centrado y estilizado como el resto de ajustes) actualiza y reinicia la app automáticamente.
- Resumen: alineados los encabezados y los bordes superior e inferior entre «Últimas partidas» y la columna derecha («Últimos tesoros» y el resumen de compañeros), con barras de desplazamiento internas en tarjetas largas.
- Cifras: «tuyos por partida» y «del equipo por partida» de Compañeros pasan a «en equipo», y la línea del gráfico indica «media por día jugado».
- Tesoros: en «Todas las semanas», se quita la ficha con el recuento total de artilugios.
- `build.bat`: al terminar de compilar `dist\FCT.exe`, abre directamente la aplicación en lugar de abrir la carpeta en el explorador.

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

[Sin publicar]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v4.4.1...HEAD
[4.4.1]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v4.4.0...v4.4.1
[4.4.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v4.3.0...v4.4.0
[4.3.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v4.2.1...v4.3.0
[4.2.1]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v4.2.0...v4.2.1
[4.2.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v4.1.0...v4.2.0
[4.1.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v4.0.0...v4.1.0
[4.0.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v3.7.0...v4.0.0
[3.7.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v3.6.0...v3.7.0
[3.6.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v3.5.0...v3.6.0
[3.5.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v3.4.1...v3.5.0
[3.4.1]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v3.4.0...v3.4.1
[3.4.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v3.3.0...v3.4.0
[3.3.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v3.2.0...v3.3.0
[3.2.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v3.1.0...v3.2.0
[3.1.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v3.0.0...v3.1.0
[3.0.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v2.2.0...v3.0.0
[2.2.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v2.1.0...v2.2.0
[2.1.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v2.0.2...v2.1.0
[2.0.2]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v2.0.1...v2.0.2
[2.0.1]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v1.5.0...v2.0.0
[1.5.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v1.4.0...v1.5.0
[1.4.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/adrc4-ua/Fortnite-Chest-Tracker/releases/tag/v1.0.0
