<div align="center">

[Español](README.md) · **English**

# Fortnite Chest Tracker (FCT)

[![version](https://img.shields.io/github/v/release/adrc4-ua/Fortnite-Chest-Tracker?style=flat-square&label=version&color=e8b04b)](../../releases/latest) [![downloads](https://img.shields.io/github/downloads/adrc4-ua/Fortnite-Chest-Tracker/total?style=flat-square&label=downloads&color=e8b04b)](../../releases) ![Windows 10 | 11](https://img.shields.io/badge/Windows-10%20%7C%2011-3a7bd5?style=flat-square) [![license](https://img.shields.io/github/license/adrc4-ua/Fortnite-Chest-Tracker?style=flat-square&label=license&color=555)](LICENSE)

**Counts every chest you open in Fortnite, match after match: window, stats, in-game overlay and your team's chests.**

<img src="docs/images/main.png" alt="Main window" width="49%">&nbsp;<img src="docs/images/report-hero.png" alt="Stats" width="49%">

[**⬇ Download FCT.exe**](../../releases/latest) · Windows 10 / 11 · free · no install · Spanish and English

</div>

---

FCT reads the replays Fortnite saves of every match and keeps count of the chests you open, the **treasure hunt treasures** and, if you want, your **teammates'** chests. Everything is saved on your PC: even when Fortnite deletes the replays, your history is kept.

- **Main window**: total, last match, today, best day, average, best match, treasures, this week's chart and your team.
- **Stats** in your browser, with tabs: overview, chests, treasures, matches, teammates and settings.
- **Overlay** on top of the game and an **OBS page**.
- **Teammates**: who you play with, wins together, each one's chests per match and a leaderboard.
- **Safe**: it only reads replay files; it never touches the game or its memory.

> It counts the chests **you** opened (and those your team opened near you). It does not show where chests are on the map.

## Getting started

1. Download **[`FCT.exe`](../../releases/latest)**. That single file is all you need.
2. In Fortnite turn on *Settings → Game → Replays → **Record Replays*** (and *Large Team Modes* if you play them). Without replays there is nothing to read.
3. Run `FCT.exe`.
   > If Windows warns you, click *More info → Run anyway*. It happens because the program isn't signed yet.
4. The welcome screen reminds you about replays and, if there are already replays on your PC, lets you **count them** or start with **your next matches only**.

<p align="center"><img src="docs/images/setup.png" alt="First-run welcome" width="520"></p>

5. Leave it running while you play:
   - **With Fortnite open**, opening FCT shows only the **overlay** and the window stays in the background, with its icon in the notification area (the **^** arrow on the taskbar). Click the icon, or open the program again, to bring it to the front; right-click for overlay, stats, settings and exit.
   - **Without Fortnite**, the window opens.
   - The **window's X** keeps it in the background if Fortnite is open; otherwise it closes FCT. The **overlay's ✕** closes FCT completely.
   - In the background, FCT closes by itself when Fortnite closes (once the last match is counted), or from **Exit** on its icon.
    During the match FCT reads the replay being recorded and adds the chests **live**; when the match ends they are confirmed.

> [!TIP]
> Matches played while FCT is closed are counted next time you open it, as long as Fortnite has not deleted those replays (it keeps a limited number). To not think about it, turn on **Open with Fortnite** in Settings.

## The main window

<p align="center"><img src="docs/images/main.png" alt="Main window" width="680"></p>

- **Top left**: your chest total, the last match (or the current one, in green, while you play) and today (chests, matches and average).
- **Right**: today, best day, average, best match, history (saved matches and since when) and treasures.
- **This week**: a chart with each day's chests, Monday to Sunday.
- **Team** (with *Teammates* on): your chests and each teammate's in the current or last match, the team total and their share.
- **Buttons**: **Overlay** (green dot = on), **Stats** and **Settings**. With several accounts on the PC, a profile picker shows up at the top.

The window can be resized and maximized: everything grows in proportion.

## Stats

The stats page opens in your browser (the **Stats** button), served by FCT at `http://localhost:7373/estadisticas`, and refreshes by itself; if you close FCT the page says so instead of showing an error. It is also saved as `estadisticas.html`. It always starts on **Overview**; the tabs are in the menu on the left. On a PC every tab fits the screen: long lists scroll inside their own box.

| Tab | What it shows |
|---|---|
| **Overview** | Chests opened (and today's), wins, matches, average, best match, best day, a chart of the last 30 days, latest matches, **latest treasures** (with each item's icon) and, with teammates, their share and who you play with most. |
| **Chests** | By chest type, other containers (ammo boxes, food boxes, loot cannons; shows whether they count) and a **by game mode** table with the treasures of each mode. |
| **Treasures** | How many (featured and gizmos), % of matches with a treasure, **treasures available now** (this week's and, with a button, the five weeks in a row), where you get your treasures and the list with the exact moment and who got them. |
| **Matches** | The latest 100: mode, placement (🏆 win, top 10 in green), eliminations, chests, treasures and, with teammates, the team. |
| **Teammates** | Team chests, the table of **all your teammates** (sorts by matches, wins, chests, average…) and the top 5 for most chests opened and best single match (see below). |
| **Settings** | The stats page's settings: language is shared with the app (changes both and restarts FCT), while the rest only change this page (show teammates and add their chests, and what counts as a chest). Also the FCT version and links to GitHub. |

<p align="center"><img src="docs/images/report-companeros.png" alt="Teammates tab of stats" width="820"></p>

<details>
<summary><b>See the other tabs</b></summary>

<p align="center"><img src="docs/images/report-resumen.png" alt="Overview" width="820"></p>
<p align="center"><img src="docs/images/report-cofres.png" alt="Chests" width="820"></p>
<p align="center"><img src="docs/images/report-tesoros.png" alt="Treasures" width="820"></p>
<p align="center"><img src="docs/images/report-partidas.png" alt="Matches" width="820"></p>

</details>

Black and gold, like the window, and it adapts to phones. The screenshots are in Spanish; changing the language in either the app or the stats page updates both.

## Teammates and team

It's on by default: on the page (stats **Settings → Show teammates**) and in the window (the app's **Settings → Teammates in the window**); each can be turned off. You get:

- **Exact**: who you play with, matches together and wins together.
- **Approximate**: each teammate's chests. They come from **your** replay, and Fortnite only records what happens **within 1,087 m of you** (measured on real replays). If a teammate goes further, their chests are not counted: they are a **minimum (≥)**.
- **Team chests**: you + your teammates, in total and per match, with each one's share; handy when you hunt for a random item that drops from chests.
- **All your teammates**: matches together, wins, chests opened, average and their share; tap a column to sort. Beside it, the top 5 for most chests opened and best single match.

With **Add their chests to your total** (off by default), your total, average, records and overlay include your team's chests. Your history does not change: turning it off shows only yours again.

## The overlay

<p align="center"><img src="docs/images/overlay-espera.png" alt="Overlay between two matches" width="300">&nbsp;&nbsp;<img src="docs/images/overlay-en-vivo.png" alt="Overlay live during a match" width="300"></p>

- **Big numbers**: your chest **total** and, in the box, the **previous** match (or the **current** one, live, while you play).
- **Today**, today's **average** per match, **previous** (during a match) and **treasures**.

To see it over the game, set Fortnite to **Windowed Fullscreen** (*Settings → Video → Window Mode*): in exclusive fullscreen Windows cannot draw on top. It only shows while Fortnite is open (not minimized) on some screen; then you can put it on any screen and spot, and it stays visible even if you use another window. It can't be dragged past the screen edges. It's on by default and turns on when Fortnite opens; turn it off with the **Overlay** button or Ctrl+Shift+O.

Streaming? While FCT is open, the same overlay is at **http://localhost:7373/** (copy it from **Settings**). In OBS, add a **browser source** with that address: transparent background, refreshes by itself.

### Shortcuts

| Shortcut | What it does |
|---|---|
| **Ctrl+Shift+O** | Show / hide the overlay. Works anywhere, even in game. |
| **Ctrl+Shift+L** | Lock / unlock the overlay. **Locked**: clicks go through and it does not move. **Unlocked**: drag it with the left button. |
| Right-click on the overlay | Hide it. |
| Overlay **✕** button (right of the padlock) | Close FCT (the app and the overlay). |

## Settings

The app's (the **Settings** button) change the window and the overlay; the stats page has its own in its **Settings** tab (except language, which is shared).

<p align="center"><img src="docs/images/settings.png" alt="Settings" width="620"></p>

- **Language**: the Windows language by default (Spanish or English; English for any other); at the top you can pick another one (synced both ways with the stats page).
- **Open with Fortnite** (on from first use): a small invisible process opens FCT in the background when Fortnite starts, and FCT closes by itself when Fortnite closes. It reads nothing from the game: it only looks at the list of running programs.
- **Open stats at startup** (off by default).
- **Teammates in the window** and **Add their chests to your total** (see above).
- **Also count as a chest**: ammo boxes, food boxes and loot cannons (off; nothing is deleted when you change it). Chests you **destroy** are always counted.
- **Chests before FCT**: an estimate added to your total. It's asked the first time and can be redone in **Settings → Recalculate**: you choose *Start from zero* / *Only saved ones* or *Work out earlier matches*:
  - **Matches this season**: looked up with your Epic name. Only if they can't be found (or your profile is private, which it tells you) do you type them in, reading them on [fortnite.gg](https://fortnite.gg/stats).
  - **Average chests per match**: that of your replays together with the **new matches** you play from then on (you choose how many: 5 by default, **10 recommended**; the more, the closer). Each replay weighs as much as a new match, so the more replays you have, the more they count. Or type it by hand.
  - **Estimate** = average × (matches this season − this season's replays already counted; replays from earlier seasons go into the average but are not subtracted). The average is rounded down only if you had no replays or 100 or more (the most Fortnite keeps). Until you play the new matches you see a provisional estimate. With no replays, the past is the estimate alone.
- **Updates**, **overlay for OBS**, **replays folder** and **Reset statistics** (with a backup).

## How it works (and why it is safe)

Fortnite can record a **replay file** of every match on your PC. FCT reads those files and nothing else.

- ✅ **Only reads replay files**: the `.replay` files in `%LOCALAPPDATA%\FortniteGame\Saved\Demos`, read-only.
- ✅ **No game modification**: the Fortnite installation folder is never touched.
- ✅ **No access to the game's memory**: it never reads, writes or injects anything into the Fortnite process.
- ✅ **No data sent anywhere**: no login, no Epic account, no telemetry. Network access: checking GitHub for new versions (can be turned off) and, only if you press **Look up** when estimating, asking fortnite-api.com for your matches this season (it sends your Epic id or name, nothing else).
- ✅ **Lightweight**: low priority ("efficiency mode" on Windows 11) and it sleeps between matches.

## Treasures

- **★★ Featured treasure.** The prizes of the week (outfit, pickaxe, vehicle, jam track, loading screen…) and the Pirate Shop Access. The server sends their **id**, so they show up with their name and type.
- **★ Treasure gizmo.** One of the five gizmos (Golden Firework, Portable Extractor, Sprite Locator, Lucky Locator, Extraction Accelerator). The replay does not say which: the **post-match treasure screen** does, and FCT reads it by itself (see below). When it could not be read, it shows as "Treasure gizmo".
- Every treasure shows the exact moment, the chest it came from and **who opened it** (you or a teammate): squad treasures are shared, so all of them count, as on the post-match screen.
- A treasure obtained from something other than a chest (loot cannon, ammo box…) is flagged with ⚑.
- **Post-match screen.** When Fortnite shows "Treasure found", FCT captures **the game window only** (even behind other windows, windowed or full screen), recognizes the icons and **deletes the image right away**: it is never saved. Everything is counted from the replay first, as always; the screen only names the gizmos and adds the ones the replay did not see (for example after your elimination), with **unknown** time and finder. If the game is minimized at that moment there is nothing to capture and the match keeps the replay data only. It does not touch the game process: the same technique as OBS window capture.
- **Replay limit:** it only holds what the game sends to you. Once you are eliminated for good (no reboot) or leave the match, the squad's later treasures are not in the replay; only the post-match screen brings them back.

Known prizes are already named. A new one shows up as "Featured treasure" and its id is added to `treasures.json`, where you can name it with Notepad.

## Profiles (several accounts on one PC)

There is no name to type: the player who recorded each replay is counted. When several accounts play on the PC:

- **Each one has its profile** with all its history, kept even when Fortnite deletes the replays (also with more than 100 already counted). Nothing gets mixed.
- **FCT follows whoever is playing**: when a match of another account starts or ends, the window, overlay and stats switch to their profile and it tells you ("X is playing now"). One player's live match is never added to the other's.
- To see another profile, pick it in the **drop-down** at the top right of the window: it stays until the next match.
- **Chests before FCT** belongs to each player: each one can work out their earlier matches without overwriting the other's.

## Updates

FCT checks GitHub for a new version at startup and **every 30 minutes** while open, and by default **installs it by itself** between two matches (your history and settings are kept). The first time you open it, if it is not the latest version, it asks whether to download it; if you say no, updates switch to *Ask*. Versions published before the rename (ChestCounter) update too: every release carries a copy of the exe under that name; after updating, the file keeps its old name but runs FCT. In Settings you can choose *Ask* or *Never* (no network access).

## Your data

Everything is saved in `%APPDATA%\FCT`. **Your history does not depend on the replays**: each match (with its chests, treasures and teammates) is saved to `data.json` as soon as it is read, and every statistic comes from there.

| File | Contents |
|---|---|
| `data.json` | Your whole history. **Back it up** if you change PC. |
| `estadisticas.html` | The stats page. |
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
├─ PostMatch/                post-match treasure screen: game window capture and icon reading
├─ Overlay/                  in-game overlay and its OBS version
├─ Reporting/                stats page (HTML)
├─ UI/                       main window, settings and controls
├─ Localization/             app language; Languages/ to add languages (see its README)
└─ Assets/                   icon
external/FortniteReplayDecompressor/   replay library (modified: see PATCHES.md)
tools/i18n.cs                creates or updates the file of a new language (dotnet run tools/i18n.cs -- <code>)
docs/images/                 screenshots of this README
```

## License and credits

[MIT](LICENSE). This project started from [ChestCounter](https://github.com/xdoofy92/ChestCounter) by xdoofy92. Replay parsing is based on [FortniteReplayDecompressor](https://github.com/Shiqan/FortniteReplayDecompressor) by Shiqan (MIT), lightly modified: see [`PATCHES.md`](external/FortniteReplayDecompressor/PATCHES.md).

Executable signing: [code signing policy](CODE_SIGNING_POLICY.md).

This project is not affiliated with, endorsed or sponsored by Epic Games. Fortnite is a registered trademark of Epic Games, Inc.
