# Third-party notices · Avisos de terceros

Fortnite Chest Tracker (FCT) is released under the [MIT License](LICENSE). This file lists everything external the
project includes, is based on or connects to, with its license or terms. It is also embedded in `FCT.exe`
(`FCT.exe --licenses` prints it).

*FCT se publica con la [licencia MIT](LICENSE). Este archivo recoge todo lo externo que el proyecto incluye, en lo que
se basa o a lo que se conecta, con su licencia o condiciones. También va dentro de `FCT.exe` (`FCT.exe --licenses` lo
muestra).*

---

## 1. Code included in FCT.exe · Código incluido en FCT.exe

| Component | Use in FCT | License |
|---|---|---|
| [FortniteReplayDecompressor](https://github.com/Shiqan/FortniteReplayDecompressor) by Shiqan (FortniteReplayReader, Unreal.Core, Unreal.Encryption, OozSharp) | Reads and decompresses Fortnite replay files. Included as source in `external/FortniteReplayDecompressor`, **modified** (every change is listed in [`PATCHES.md`](external/FortniteReplayDecompressor/PATCHES.md)). | MIT, © 2020-2026 Shiqan (full text below) |
| [.NET runtime](https://github.com/dotnet/runtime) and [Windows Forms](https://github.com/dotnet/winforms) | `FCT.exe` is self-contained: it carries the .NET runtime so nothing has to be installed. Includes `Microsoft.Extensions.Logging.Abstractions`, used by the replay library. | MIT, © .NET Foundation and Contributors. Their own third-party notices: [runtime](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT), [winforms](https://github.com/dotnet/winforms/blob/main/THIRD-PARTY-NOTICES.TXT) |
| [C#/WinRT Windows SDK projection](https://github.com/microsoft/CsWinRT) (`Microsoft.Windows.SDK.NET`) | Windows APIs used to read the end-of-match treasure screen (Windows.Graphics.Capture). | MIT, © Microsoft Corporation |

## 2. Origin of the project · Origen del proyecto

FCT is a derived work (fork) of [ChestCounter](https://github.com/xdoofy92/ChestCounter), published by xdoofy92
under the MIT license (© 2026 LED-N). FCT took its idea (counting the chests opened in Fortnite from its replay
files), its code and its history up to 11 November 2026, and has evolved separately since then (treasures and the
end-of-match screen, teammates, profiles, estimate of earlier matches, interface, overlay, start with Fortnite,
distribution…). Part of ChestCounter's code is still in FCT, so its copyright notice is kept in [LICENSE](LICENSE),
as the MIT license requires, and its full text is below. The commits of this repository up to 11 November 2026
(up to "pend") come from ChestCounter and are the work of its author.

*FCT es una obra derivada (fork) de ChestCounter, publicado por xdoofy92 con licencia MIT (© 2026 LED-N). FCT tomó su
idea (contar los cofres abiertos en Fortnite a partir de sus repeticiones), su código y su historial hasta el 11 de
noviembre de 2026, y desde entonces ha evolucionado por separado (tesoros y pantalla de fin de partida, compañeros,
perfiles, estimado de partidas previas, interfaz, overlay, apertura con Fortnite, distribución…). Parte del código de
ChestCounter sigue en FCT, así que su aviso de copyright se conserva en LICENSE, como exige la licencia MIT, y su texto
completo está más abajo. Los commits de este repositorio hasta el 11 de noviembre de 2026 (hasta «pend») vienen de
ChestCounter y son obra de su autor.*

## 3. Online services · Servicios en línea

FCT has no login, no telemetry and no analytics. It only connects to these services, as described in the
[privacy policy](CODE_SIGNING_POLICY.md#privacy-policy):

| Service | When | What is sent |
|---|---|---|
| [GitHub](https://github.com) (api.github.com) | To check for and download new versions of FCT. Can be turned off (*Settings → Updates → Never*). | Nothing about the player: a standard request for the latest release of this repository. |
| [Fortnite-API](https://fortnite-api.com) ([docs](https://dash.fortnite-api.com/)) | Only when the player asks for it, to estimate the chests opened before using FCT: it looks up the player's Battle Royale matches **in the current season** (public stats). | The Epic account id or name the player confirms, nothing else. Requests carry the project's own free API key, as Fortnite-API requires for stats. Fortnite-API is a non-official service, not endorsed by Epic Games; FCT is not affiliated with it. If the profile is private or the lookup fails, the player can type the number instead. |
| [fortnite.gg](https://fortnite.gg/stats) | Never contacted by FCT. It is only **suggested** as a website the player can visit by themselves to read their number of matches and type it by hand, when the lookup above fails. | Nothing. FCT does not access, scrape or automate fortnite.gg in any way, and is not affiliated with it. |

The stats page and the OBS overlay are served on the player's own PC (`localhost`) and never leave it.

## 4. Code signing · Firma del código

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by
[SignPath Foundation](https://signpath.org/). See the [code signing policy](CODE_SIGNING_POLICY.md).

## 5. Trademarks · Marcas

Fortnite Chest Tracker is an independent fan project. It is not affiliated with, endorsed, sponsored or approved by
Epic Games, Inc. Fortnite, Epic Games and their logos are trademarks or registered trademarks of Epic Games, Inc. in
the United States of America and elsewhere. Names of in-game items (for example treasure names) are used only to
describe what the player obtained. FCT only reads the replay files the game saves on the player's PC and the system's
list of running programs; it does not modify the game, read its memory or interact with its anti-cheat.

*Fortnite Chest Tracker es un proyecto independiente de fans. No está afiliado a Epic Games, Inc., ni cuenta con su
respaldo, patrocinio o aprobación. Fortnite, Epic Games y sus logotipos son marcas comerciales o registradas de Epic
Games, Inc. en EE. UU. y otros países.*

GitHub is a trademark of GitHub, Inc. SignPath is a trademark of SignPath GmbH. Other names belong to their owners.

---

## License texts · Textos de licencia

### FortniteReplayDecompressor

```
MIT License

Copyright (c) 2020-2026 Shiqan

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### ChestCounter

```
MIT License

Copyright (c) 2026 LED-N

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### .NET runtime, Windows Forms and C#/WinRT

```
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors (.NET runtime, Windows Forms)
Copyright (c) Microsoft Corporation (C#/WinRT)

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
