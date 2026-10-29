# Windows launcher repair for 0.10.2h

The original h Windows archive's launcher resolves adjacent paths in parameter defaults. In a fresh Windows PowerShell 5.1 process, `$PSScriptRoot` is empty during that binding step, causing `Join-Path` to fail before connecting to the server. Helper tests dot-sourced the file from another script, which supplied a caller script root and hid this failure.

The corrected launcher resolves default invitation/executable paths in its script body. Its game arguments, private environment-based room key, invitation format and server protocol stay the same. Existing installations need only replace `Play-AscendanceMultiplayer.ps1`; the game and invitation can stay in place.

Corrected launcher source: `900ef329c3fbe4dda7df8fdd3a47f08116453f99`, SHA-256 `a4fed209aef94cde891a528b5d6d29601892b39a9b440635982fb7fab50591e1`.

Client-only installer source: `5bd137a753cc22ce2d63739fb76adcfef0308305`, SHA-256 `219a5a1bb67b29a9033b898cb31d3566c027197ac7fca6c277f9c9c7b41970a0`. It verifies the original immutable h archive and runtime provenance, then overlays the separately pinned corrected launcher before copying the private invitation. No server update or Unity rebuild is required.

[Actual Windows PowerShell 5.1 regression](https://github.com/RubberDuckieGod/ascendance-unity/actions/runs/40807452940) reproduced the original error (4/4 checks) and passed the corrected launcher (20/20 checks), including actual `.cmd` → PowerShell → synthetic native executable, paths with spaces, exact arguments and private environment inheritance. [Expected/actual evidence and limits](WINDOWS_LAUNCHER_ACCEPTANCE_0102H.json). Local launcher fixtures passed 10/10 and client-installer fixtures passed 15/15.

For an already installed game, run this in normal Windows PowerShell. Enter the path to the extracted `Windows` folder when prompted:

```powershell
$ErrorActionPreference = 'Stop'
$asc_game = Read-Host 'Full path to the extracted Windows game folder'
if (-not (Test-Path -LiteralPath (Join-Path $asc_game 'Ascendance.exe') -PathType Leaf)) { throw 'Choose the folder containing Ascendance.exe' }
$asc_fixed = Join-Path $env:TEMP 'ascendance-launcher-fixed.ps1'
Invoke-WebRequest 'https://raw.githubusercontent.com/RubberDuckieGod/ascendance-unity/900ef329c3fbe4dda7df8fdd3a47f08116453f99/tools/Play-AscendanceMultiplayer.ps1' -OutFile $asc_fixed -UseBasicParsing
if ((Get-FileHash $asc_fixed -Algorithm SHA256).Hash -ne 'a4fed209aef94cde891a528b5d6d29601892b39a9b440635982fb7fab50591e1') { throw 'Download checksum mismatch' }
Copy-Item -LiteralPath $asc_fixed -Destination (Join-Path $asc_game 'Play-AscendanceMultiplayer.ps1') -Force
Start-Process (Join-Path $asc_game 'Play-AscendanceMultiplayer.cmd')
```

Share the complete corrected client folder and invitation privately with trusted players. Never publish invitation contents or player credentials. Windows gameplay and authenticated friend connections still require acceptance; launcher tests use only synthetic local data. A separate Windows PowerShell 5.1 limitation was observed for game folders containing square brackets: `Start-Process -WorkingDirectory` can interpret those characters as a wildcard. Keep the client in its default generated folder or another path without square brackets; bracket paths are not certified by this repair.
