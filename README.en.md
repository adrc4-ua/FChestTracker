<div align="center">

[Español](README.md) · **English**

# Fortnite Chest Tracker (FCT)

[![version](https://img.shields.io/github/v/release/adrc4-ua/FChestTracker?style=flat-square&label=version&color=e8b04b)](../../releases/latest) [![downloads](https://img.shields.io/github/downloads/adrc4-ua/FChestTracker/total?style=flat-square&label=downloads&color=e8b04b)](../../releases) ![Windows 10 | 11](https://img.shields.io/badge/Windows-10%20%7C%2011-3a7bd5?style=flat-square) [![license](https://img.shields.io/github/license/adrc4-ua/FChestTracker?style=flat-square&label=license&color=555)](LICENSE)

**Counts every chest you open in Fortnite, match after match: window, report, in-game overlay and your team's chests.**

<img src="docs/images/main.png" alt="Main window" width="49%">&nbsp;<img src="docs/images/report-hero.png" alt="Report" width="49%">

[**⬇ Download FCT.exe**](../../releases/latest) · Windows 10 / 11 · free · no install · 10 languages

</div>

---

FCT reads the replays Fortnite saves of every match and keeps count of the chests you open, the **treasure hunt treasures** and, if you want, your **teammates'** chests. Everything is saved on your PC: even when Fortnite deletes the replays, your history is kept.

- **Main window**: total, last match, today, best day, average, best match, treasures, 7-day chart and your team.
- **Report** in your browser, with tabs: overview, chests, treasures, matches and teammates.
- **Overlay** on top of the game and an **OBS page**.
- **Teammates**: who you play with, wins together, each one's chests per match and a leaderboard.
- **Safe**: it only reads replay files; it never touches the game or its memory.

> It counts the chests **you** opened (and those your team was seen opening). It does not show where chests are on the map.

## Getting started

1. Download **[`FCT.exe`](../../releases/latest)**. That single file is all you need.
2. In Fortnite turn on *Settings → Game → Replays → **Record Replays*** (and *Large Team Modes* if you play them). Without replays there is nothing to read.
3. Run `FCT.exe`.
   > Windows may show *"Windows protected your PC"* because the exe is not signed. Click *More info → Run anyway*.
4. The welcome screen reminds you about replays and, if there are already replays on your PC, lets you **count them** or start with **your next matches only**.

<p align="center"><img src="docs/images/setup.png" alt="First-run welcome" width="520"></p>

5. Leave it running while you play. When it opens only the **overlay** shows: the main window stays in the background, with its icon in the notification area (the **^** arrow on the taskbar). Click the icon, or open the program again, to bring it to the front; right-click for overlay, report, settings and exit. On first use, or with the overlay turned off, the window opens as usual. During the match FCT reads the replay being recorded and adds the chests **live**; when the match ends they are confirmed.

> [!TIP]
> Matches played while FCT is closed are counted next time you open it, as long as Fortnite has not deleted those replays (it keeps a limited number). To not think about it, turn on **Open with Fortnite** in Settings.

## The main window

<p align="center"><img src="docs/images/main.png" alt="Main window" width="680"></p>

- **Top left**: your chest total, the last match (or the current one, in green, while you play) and the session.
- **Right**: today, best day, average, best match, history (saved matches and since when) and treasures.
- **Last 7 days**: a chart with each day's chests.
- **Team** (with *Teammates* on): your chests and each teammate's in the current or last match, the team total and their share.
- **Buttons**: **Overlay** (green dot = on), **Report** and **Settings**. With several accounts on the PC, a profile picker shows up at the top.

The window can be resized and maximized: everything grows in proportion.

## The report

`report.html` opens in your browser (the **Report** button) and refreshes by itself. It always starts on **Overview**; the tabs are at the top:

| Tab | What it shows |
|---|---|
| **Overview** | Chest total, session, Victory Royales (and %), top 10s, matches, average, best match, best day, treasures, 30-day chart and the latest matches. |
| **Chests** | By chest type, other containers (ammo boxes, food boxes, loot cannons; shows whether they count) and a **by game mode** table. |
| **Treasures** | How many (featured and gizmos), % of matches with a treasure, where they come from, the list with the exact moment and who got them, and every treasure hunt prize by week. |
| **Matches** | The latest 100: mode, placement (🏆 win, top 10 in green), eliminations, chests, treasures and, with teammates, the team. |
| **Teammates** | Team chests, **top teammates**, the **per match** table and all your teammates (see below). |

<p align="center"><img src="docs/images/report-companeros.png" alt="Teammates tab of the report" width="820"></p>

<details>
<summary><b>See the other tabs</b></summary>

<p align="center"><img src="docs/images/report-resumen.png" alt="Overview" width="820"></p>
<p align="center"><img src="docs/images/report-cofres.png" alt="Chests" width="820"></p>
<p align="center"><img src="docs/images/report-tesoros.png" alt="Treasures" width="820"></p>
<p align="center"><img src="docs/images/report-partidas.png" alt="Matches" width="820"></p>

</details>

It uses the window's colors (dark, or light when Windows is in light mode) and adapts to phones. The screenshots are in Spanish; the report follows the app's language.

## Teammates and team

Turn it on in **Settings → Teammates in the report** (off by default). You get:

- **Exact**: who you play with, matches together and wins together.
- **Approximate**: each teammate's chests. They come from **your** replay, and Fortnite only records what happens **within 1,087 m of you** (measured on real replays). If a teammate goes further, their chests are not counted: they are a **minimum (≥)**.
- **Team chests**: you + your teammates, in total and per match, with each one's share; handy when you hunt for a random item that drops from chests.
- **Top teammates**: most chests seen, best average, best single match and most wins together.

With **Add their chests to your total** (also off), your total, average, records and overlay include your team's chests. Your history does not change: turning it off shows only yours again.

## The overlay

<p align="center"><img src="docs/images/overlay-espera.png" alt="Overlay between two matches" width="300">&nbsp;&nbsp;<img src="docs/images/overlay-en-vivo.png" alt="Overlay live during a match" width="300"></p>

- **Big numbers**: your chest **total** and, in the box, the **previous** match (or the **current** one, live, while you play).
- **Session**, session **average**, **previous** (during a match) and **treasures**.

To see it over the game, set Fortnite to **Windowed Fullscreen** (*Settings → Video → Window Mode*): in exclusive fullscreen Windows cannot draw on top. The overlay starts **off**: turn it on with the **Overlay** button or Ctrl+Shift+O.

Streaming? While FCT is open, the same overlay is at **http://localhost:7373/** (copy it from **Settings**). In OBS, add a **browser source** with that address: transparent background, refreshes by itself.

### Shortcuts

| Shortcut | What it does |
|---|---|
| **Ctrl+Shift+O** | Show / hide the overlay. Works anywhere, even in game. |
| **Ctrl+Shift+L** | Lock / unlock the overlay. **Locked**: clicks go through and it does not move. **Unlocked**: drag it with the left button. |
| Right-click on the overlay | Hide it. |
| Overlay **✕** button (right of the padlock) | Close FCT (the app and the overlay). |

## Settings

<p align="center"><img src="docs/images/settings.png" alt="Settings" width="620"></p>

- **Language**: the Windows language by default; at the top you can pick another (Spanish, English, Portuguese, Russian, French, German, Italian, Polish, Turkish or Japanese).
- **Open with Fortnite**: a small invisible process opens FCT in the background when Fortnite starts and closes it 30 s after Fortnite closes. It reads nothing from the game: it only looks at the list of running programs.
- **Open the report at startup** (off by default).
- **Teammates in the report** and **Add their chests to your total** (see above).
- **Also count as a chest**: ammo boxes, food boxes and loot cannons (off; nothing is deleted when you change it). Chests you **destroy** are always counted.
- **Chests before FCT**: an estimate added to your total.
- **Updates**, **overlay for OBS**, **replays folder** and **Reset statistics** (with a backup).

## How it works (and why it is safe)

Fortnite can record a **replay file** of every match on your PC. FCT reads those files and nothing else.

- ✅ **Only reads replay files**: the `.replay` files in `%LOCALAPPDATA%\FortniteGame\Saved\Demos`, read-only.
- ✅ **No game modification**: the Fortnite installation folder is never touched.
- ✅ **No access to the game's memory**: it never reads, writes or injects anything into the Fortnite process.
- ✅ **No data sent anywhere**: no login, no Epic account, no telemetry. The only network access is checking GitHub for new versions (can be turned off).
- ✅ **Lightweight**: low priority ("efficiency mode" on Windows 11) and it sleeps between matches.

## Treasures

- **★★ Featured treasure.** The prizes of the week (outfit, pickaxe, vehicle, jam track, loading screen…) and the Pirate Shop Access. The server sends their **id**, so they show up with their name and type.
- **★ Treasure gizmo.** One of the five gizmos (Golden Firework, Portable Extractor, Sprite Locator, Lucky Locator, Extraction Accelerator); the replay does not say which.
- Every treasure shows the exact moment, the chest it came from and **who opened it** (you or a teammate): squad treasures are shared, so all of them count, as on the post-match screen.
- A treasure obtained from something other than a chest (loot cannon, ammo box…) is flagged with ⚑.
- **Limit:** the replay only holds what the game sends to you. Once you are eliminated for good (no reboot) or you leave the match, the treasures the squad gets afterwards **are not in the replay** and cannot be counted; the match shows ⚠ in the report. Nothing on the PC keeps them (not even the game log); the only complete source is the post-match screen.

Known prizes are already named. A new one shows up as "Featured treasure" and its id is added to `treasures.json`, where you can name it with Notepad.

## Profiles (several accounts on one PC)

There is no name to type: the player who recorded each replay is counted. With replays from several accounts, each one gets its profile and a **drop-down** at the top right picks which one is shown.

## Updates

At startup, FCT checks GitHub for a new version and, by default, **installs it by itself** between two matches (your history and settings are kept). In Settings you can choose *Ask* or *Never* (no network access).

## Your data

Everything is saved in `%APPDATA%\FCT`. **Your history does not depend on the replays**: each match (with its chests, treasures and teammates) is saved to `data.json` as soon as it is read, and every statistic comes from there.

| File | Contents |
|---|---|
| `data.json` | Your whole history. **Back it up** if you change PC. |
| `report.html` | The report. |
| `matches.csv` | One line per match, for Excel or Google Sheets. |
| `config.json` | Settings, profile, replays folder and overlay position. |
| `treasures.json` | Names of the treasure hunt prizes. |
| `overlay.html` | Copy of the overlay (for OBS, http://localhost:7373/ is better). |
| `errors.log` | Replays that could not be read, to attach to a bug report. |

## FAQ

**Nothing is counted.** Check that replays are on in Fortnite. The window tells you if the replays folder is missing or empty.

**Why are my teammates' chests low?** Only those opened within 1,087 m of you are seen. It is a minimum, not their total.

**How do I start over?** **Settings → Reset statistics…** (with confirmation and a `data-backup-….json` backup).

**Creative modes?** Only Battle Royale matches are counted.

**"Cannot read this replay".** A Fortnite update may change the format. Matches already counted are kept. Open an [issue](../../issues) and attach `errors.log`.

**Can a match be counted twice?** No, even if you rename or save the replay.

## Command line options

```
FCT.exe --demos <folder>   another replays folder
FCT.exe --data <folder>    another data folder
FCT.exe --once             analyze the finished replays and quit
FCT.exe --reset-config     run the first-time setup again
FCT.exe --overlay-preview <file.png>   save overlay previews and quit
```

## Building from source

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (Windows x64) and double-click **`build.bat`**: the exe goes to `dist\FCT.exe`. You can also open `FCT.slnx` in Visual Studio or VS Code. GitHub Actions builds the exe on every push; when a release is published on GitHub, it attaches `FCT.exe` to it. What changed in each version is in [CHANGELOG.md](CHANGELOG.md) (Spanish).

### Project structure

```
src/                         the app: FCT.csproj → FCT.exe
├─ Program.cs                startup and command line options
├─ App/                      version, updates, start with Fortnite, data move, error log
├─ Analysis/                 replay reading: chests, treasures, prizes, profiles
├─ Data/                     what is saved: matches, settings and the JSON store
├─ Monitoring/               the background process (watches replays, overlay and updates)
├─ Overlay/                  in-game overlay and its OBS version
├─ Reporting/                HTML report
├─ UI/                       main window, settings and controls
├─ Localization/             texts in the 10 languages
└─ Assets/                   icon
external/FortniteReplayDecompressor/   replay library (modified: see PATCHES.md)
docs/images/                 screenshots of this README
```

## License and credits

[MIT](LICENSE). This project started from [ChestCounter](https://github.com/xdoofy92/ChestCounter) by xdoofy92. Replay parsing is based on [FortniteReplayDecompressor](https://github.com/Shiqan/FortniteReplayDecompressor) by Shiqan (MIT), lightly modified: see [`lib/PATCHES.md`](lib/PATCHES.md).

This project is not affiliated with, endorsed or sponsored by Epic Games. Fortnite is a registered trademark of Epic Games, Inc.
