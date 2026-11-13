# FortniteReplayDecompressor in FCT

This folder is [FortniteReplayDecompressor](https://github.com/Shiqan/FortniteReplayDecompressor) by Shiqan, MIT
license ([LICENSE](LICENSE)), at commit `76f74c1`, with the small changes below. Everything else FCT needs from replays
lives in FCT's own code (`src/Analysis/Replay/`, which builds on the library's virtual methods, and the models of
`src/ReplayModels/`).

## Changes

1. **Group aliases** — `Unreal.Core/ReplayAliases.cs` (new) and `Unreal.Core/NetFieldParser.cs`: the application can
   map a group or ClassNetCache path the parser has no type for onto one it has (new chest blueprints every season,
   loot cannon variants). Resolved once per path.
2. **Level-placed actors** — `Unreal.Core/Models/NetGuidCache.cs`: an actor named after its class plus an instance tail
   ("…_UAID_…" or "…_&lt;hex&gt;_&lt;n&gt;") finds the group of exactly that class; and when a new group arrives, paths that
   had failed are looked at again (an actor can arrive before its group).
3. **Replays being recorded** — `Unreal.Core/ReplayReader.cs`: a live replay is accepted when its encryption key is
   already written (Fortnite writes it from the start).
4. **Temporary end of a bit reader** — `Unreal.Core/BitReader.cs`: `SetTempEnd` saves the current end before it can
   fail, so the matching `RestoreTempEnd` never moves backwards (that made some replays loop until the time limit).
5. **Container model** — `FortniteReplayReader/Models/NetFieldExports/Items/BaseContainer.cs`: `SearchingPawn` (who
   searched or broke it), and nullable `bAlreadySearched` / `SearchAnimationCount` (not sent ≠ false).
6. **Gameplay cues** — `FortniteReplayReader/Models/NetFieldExports/RPC/GameplayCue.cs`: only read in Debug mode.
7. **Build** — `Directory.Build.props` (new): silences the original code's compiler warnings, without touching its
   project files.

Every change is marked with an "FCT" comment in the code. These files keep the library's MIT license; the rest of FCT
is GPL-3.0 (see the project's [NOTICE.md](../../NOTICE.md) and [THIRD_PARTY_NOTICES.md](../../THIRD_PARTY_NOTICES.md)).
