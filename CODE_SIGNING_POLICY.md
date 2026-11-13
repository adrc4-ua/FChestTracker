# Code signing policy

*En español, más abajo.*

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).

## What is signed

Only `FCT.exe`, the single Windows executable attached to the [releases](../../releases) of this repository. It is
built from this repository's source code by the GitHub Actions workflow [`.github/workflows/build.yml`](.github/workflows/build.yml)
and submitted to SignPath from that workflow. Nothing built outside GitHub Actions is signed, and only release builds
(tags `vX.Y.Z`) are signed.

The third-party code included in the executable is listed in the [README](README.en.md#license-and-credits)
(FortniteReplayDecompressor, MIT, with the changes described in `external/FortniteReplayDecompressor/PATCHES.md`).

## Team roles

| Role | Members |
|---|---|
| Committers and reviewers | [@adrc4-ua](https://github.com/adrc4-ua) |
| Approvers | [@adrc4-ua](https://github.com/adrc4-ua) |

Every signing request is approved by hand by an approver. All members use multi-factor authentication on GitHub and
SignPath.

## Privacy policy

This program will not transfer any information to other networked systems unless specifically requested by the user
or the person installing or operating it. In detail:

- **Updates:** it asks GitHub (api.github.com) whether there is a newer release. This can be turned off in
  *Settings → Updates*.
- **Earlier matches:** only when the user presses **Look up** in *Chests before FCT*, it asks fortnite-api.com for the
  user's matches this season, sending the Epic account id or name typed by the user, and nothing else.
- **Stats page and OBS overlay:** served on the user's own PC (`localhost`) only; nothing leaves the computer.

It has no login, no telemetry and no analytics. Everything it records stays in the user's data folder.

---

## Política de firma (resumen)

Firma de código gratuita de [SignPath.io](https://about.signpath.io/), certificado de [SignPath Foundation](https://signpath.org/).

- **Qué se firma:** solo `FCT.exe` de las releases, compilado desde este repositorio por GitHub Actions y enviado a
  SignPath desde ese flujo. Cada firma la aprueba a mano el responsable del proyecto.
- **Privacidad:** el programa no envía nada salvo que lo pidas: buscar versiones nuevas en GitHub (se puede desactivar)
  y, solo si pulsas **Buscar** en *Cofres antes de FCT*, tus partidas de la temporada a fortnite-api.com (tu id o
  nombre de Epic, nada más). La página de estadísticas y el overlay para OBS solo funcionan en tu PC. Sin inicio de
  sesión ni telemetría.
