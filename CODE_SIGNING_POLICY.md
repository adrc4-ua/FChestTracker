# Code signing policy

*En español, más abajo.*

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).

## What is signed

Only `FCT.exe`, the single Windows executable attached to the [releases](../../releases) of this repository. It is
built from this repository's source code by the GitHub Actions workflow [`.github/workflows/build.yml`](.github/workflows/build.yml)
and submitted to SignPath from that workflow ([artifact configuration](.signpath/artifact-configuration.xml)). Nothing
built outside GitHub Actions is signed, and only release builds (tags `vX.Y.Z`) are signed. The product name
(*Fortnite Chest Tracker*) and the version are in the executable's metadata.

The project uses the GNU GPL version 3 (GPL-3.0-only, OSI-approved; its additional terms in [NOTICE.md](NOTICE.md)
are the attribution and origin terms its section 7 allows), with no commercial dual-licensing. Every third-party
component in the executable uses the MIT license (OSI-approved). The third-party code is listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md):
FortniteReplayDecompressor (MIT, with the changes described in `external/FortniteReplayDecompressor/PATCHES.md`) and
the .NET runtime (MIT).

## Team roles

| Role | Members |
|---|---|
| Authors (committers) | [@adrc4-ua](https://github.com/adrc4-ua) |
| Reviewers | [@adrc4-ua](https://github.com/adrc4-ua) |
| Approvers | [@adrc4-ua](https://github.com/adrc4-ua) |

Every signing request is approved by hand by an approver on SignPath. External contributions are reviewed before they
are merged. All members use multi-factor authentication on GitHub and SignPath.

## Privacy policy

This program will not transfer any information to other networked systems unless specifically requested by the user
or the person installing or operating it. In detail:

- **Updates:** it asks GitHub (api.github.com) whether there is a newer release and downloads it from GitHub. This can
  be turned off in *Settings → Updates → Never*.
- **Earlier matches:** only when the user asks for it (estimate of the chests opened before using FCT), it asks
  [Fortnite-API](https://fortnite-api.com) for the user's matches in the current season, sending the Epic account id or
  name confirmed by the user, and nothing else.
- **Stats page and OBS overlay:** served on the user's own PC (`localhost`) only; nothing leaves the computer.

It has no login, no telemetry and no analytics. Everything it records stays in the user's data folder
(`%APPDATA%\FCT`).

## Uninstall

In FCT, **Settings → Uninstall…** (or `FCT.exe --uninstall`): FCT closes and removes everything it leaves in
Windows, with nothing to turn off first: `FCT.exe` itself, the scheduled task "Fortnite Chest Tracker" of *Open with
Fortnite*, the startup entries of earlier versions and update leftovers. The user's data (`%APPDATA%\FCT`) is kept;
it can be deleted by hand.

---

## Política de firma de código

Firma de código gratuita de [SignPath.io](https://about.signpath.io/), certificado de [SignPath Foundation](https://signpath.org/).

- **Qué se firma:** solo `FCT.exe` de las releases, compilado desde este repositorio por GitHub Actions y enviado a
  SignPath desde ese flujo. Nada compilado fuera de GitHub Actions se firma.
- **Licencias:** el proyecto es GNU GPL versión 3 (aprobada por la OSI), con las condiciones de atribución que permite
  su sección 7 ([NOTICE.md](NOTICE.md)), sin licencia comercial aparte; el código de terceros del exe es MIT (ver
  [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)).
- **Funciones del equipo:** autor, revisor y aprobador: [@adrc4-ua](https://github.com/adrc4-ua). Cada firma la
  aprueba a mano el aprobador; todos usan autenticación en dos pasos en GitHub y SignPath.
- **Privacidad:** el programa no envía nada salvo que lo pidas: buscar y descargar versiones nuevas en GitHub (se
  puede desactivar en *Configuración → Actualizaciones → Nunca*) y, solo si lo pides al estimar los cofres de antes,
  tus partidas de la temporada a Fortnite-API (tu id o nombre de Epic, nada más). La página de estadísticas y el
  overlay para OBS solo funcionan en tu PC. Sin inicio de sesión ni telemetría.
- **Desinstalar:** **Configuración → Desinstalar…** (o `FCT.exe --uninstall`) borra el exe, la tarea programada de
  *Abrir con Fortnite*, las entradas de inicio y los restos de actualizaciones, sin desactivar nada antes. Tus datos
  (`%APPDATA%\FCT`) se quedan; si quieres, bórralos a mano.
