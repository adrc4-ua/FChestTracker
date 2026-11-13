# Third-party notices · Avisos de terceros

Fortnite Chest Tracker (FCT), Copyright (C) 2026 Adrián Ruiz, is released under the [GNU GPL version 3](LICENSE)
with the additional terms in [NOTICE.md](NOTICE.md) (versions up to 4.4.1: MIT). This file lists the third-party code
FCT includes, with its license, and where the project comes from; the third-party parts keep their own licenses. What
FCT connects to is in the [privacy policy](CODE_SIGNING_POLICY.md#privacy-policy). It is also embedded in `FCT.exe` (`FCT.exe --licenses` prints it).

*FCT, Copyright (C) 2026 Adrián Ruiz, se publica con la licencia GNU GPL versión 3 con las condiciones adicionales de
NOTICE.md (hasta la 4.4.1: MIT). Este archivo recoge el código de terceros que incluye FCT, con su licencia, y de dónde viene el
proyecto; las partes de terceros conservan sus propias licencias. A qué se conecta FCT está en la política de privacidad. También va
dentro de `FCT.exe` (`FCT.exe --licenses` lo muestra).*

---

## 1. Code included in FCT.exe · Código incluido en FCT.exe

| Component | Use in FCT | License |
|---|---|---|
| [FortniteReplayDecompressor](https://github.com/Shiqan/FortniteReplayDecompressor) by Shiqan (FortniteReplayReader, Unreal.Core, Unreal.Encryption, OozSharp) | Reads and decompresses Fortnite replay files. Included as source in `external/FortniteReplayDecompressor`, **modified** (every change is listed in [`PATCHES.md`](external/FortniteReplayDecompressor/PATCHES.md)). | MIT, © 2020-2026 Shiqan (full text below) |
| [.NET runtime](https://github.com/dotnet/runtime) and [Windows Forms](https://github.com/dotnet/winforms) | `FCT.exe` is self-contained: it carries the .NET runtime so nothing has to be installed. Includes `Microsoft.Extensions.Logging.Abstractions`, used by the replay library. | MIT, © .NET Foundation and Contributors. Their own third-party notices: [runtime](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT), [winforms](https://github.com/dotnet/winforms/blob/main/THIRD-PARTY-NOTICES.TXT) |
| [C#/WinRT Windows SDK projection](https://github.com/microsoft/CsWinRT) (`Microsoft.Windows.SDK.NET`) | Windows APIs used to read the end-of-match treasure screen (Windows.Graphics.Capture). | MIT, © Microsoft Corporation |

## 2. Origin of the project · Origen del proyecto

FCT started as a fork of [ChestCounter](https://github.com/xdoofy92/ChestCounter), by xdoofy92, which gave it its
idea: counting the chests opened in Fortnite from the game's replay files. From the version after 4.4.1 all of FCT's
code is its own. Versions up to 4.4.1, and the first commit of this repository ("Base de código añadida"), include
ChestCounter code under its MIT license (© 2026 LED-N), with its notice.

*FCT empezó como un fork de ChestCounter, de xdoofy92, que le dio la idea: contar los cofres abiertos en Fortnite a partir
de las repeticiones del juego. Desde la versión siguiente a la 4.4.1 todo el código de FCT es propio. Las versiones hasta
la 4.4.1, y el primer commit de este repositorio («Base de código añadida»), incluyen código de ChestCounter con su
licencia MIT (© 2026 LED-N) y su aviso.*

## 3. Trademarks · Marcas

Fortnite Chest Tracker is an independent fan project. It is not affiliated with, endorsed, sponsored or approved by
Epic Games, Inc. Fortnite, Epic Games and their logos are trademarks or registered trademarks of Epic Games, Inc. in
the United States of America and elsewhere. Names of in-game items (for example treasure names) are used only to
describe what the player obtained. FCT only reads the replay files the game saves on the player's PC and the system's
list of running programs; it does not modify the game, read its memory or interact with its anti-cheat.

*Fortnite Chest Tracker es un proyecto independiente de fans. No está afiliado a Epic Games, Inc., ni cuenta con su
respaldo, patrocinio o aprobación. Fortnite, Epic Games y sus logotipos son marcas comerciales o registradas de Epic
Games, Inc. en EE. UU. y otros países.*

Fortnite-API and fortnite.gg are unofficial services, not related to this project. GitHub is a trademark of GitHub,
Inc. SignPath is a trademark of SignPath GmbH. Other names belong to their owners.

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
