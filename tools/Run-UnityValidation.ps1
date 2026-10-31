#requires -Version 5.1
[CmdletBinding()]
param(
    [string]$Editor = $env:UNITY_EDITOR_PATH,
    [string]$Artifacts,
    [ValidateRange(60,7200)][int]$TimeoutSeconds = 1800,
    [switch]$BuildLinuxServer,
    [switch]$BuildWindows,
    [switch]$RequireInstalledArt,
    [switch]$RequireAllLocationArt
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectDir = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!$Editor -or ![IO.Path]::IsPathRooted($Editor) -or !(Test-Path -LiteralPath $Editor -PathType Leaf)) {
    throw 'Set -Editor to the absolute path of licensed Unity 6000.6.5f1 (Editor/Unity.exe).'
}
$Editor = (Resolve-Path -LiteralPath $Editor).Path
if ((Get-Content -LiteralPath (Join-Path $projectDir 'ProjectSettings/ProjectVersion.txt') -Raw) -notmatch '(?m)^m_EditorVersion: 6000\.6\.5f1\r?$') {
    throw 'ProjectVersion.txt must pin Unity 6000.6.5f1.'
}
if ($RequireAllLocationArt) { $RequireInstalledArt = $true }
$artManifestHash = ''
if ($RequireInstalledArt) {
    $manifestPath = $env:ASCENDANCE_ART_MANIFEST
    $absoluteManifest = ![string]::IsNullOrWhiteSpace($manifestPath) -and [IO.Path]::IsPathRooted($manifestPath)
    # On Windows a drive-relative path (C:art.json or \art.json) is rooted,
    # but it still depends on the process's current drive or directory.
    if ([IO.Path]::DirectorySeparatorChar -eq '\' -and $absoluteManifest) {
        $absoluteManifest = $manifestPath -match '^(?:[A-Za-z]:[\\/]|[\\/]{2}[^\\/]+[\\/][^\\/]+(?:[\\/]|$))'
    }
    if (!$absoluteManifest -or !(Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw 'Required art validation needs ASCENDANCE_ART_MANIFEST set to an absolute existing manifest file.'
    }
    $artManifestHash = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
}
if (!$Artifacts) { $Artifacts = Join-Path $projectDir ('artifacts/unity-validation/run-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '-' + [Guid]::NewGuid().ToString('N')) }
$Artifacts = [IO.Path]::GetFullPath($Artifacts)
if (Test-Path -LiteralPath $Artifacts) { throw 'Choose a new artifacts directory; existing results cannot satisfy a new run.' }
[void][IO.Directory]::CreateDirectory($Artifacts)
Write-Host "Unity validation artifacts: $Artifacts"
if (Get-Command git -ErrorAction SilentlyContinue) {
    try {
        $revision = & git -C $projectDir rev-parse --verify HEAD 2>$null
        if ($LASTEXITCODE -eq 0) {
            [IO.File]::WriteAllText((Join-Path $Artifacts 'source-revision.txt'), ($revision -join "`n"))
            $worktree = & git -C $projectDir status --porcelain
            [IO.File]::WriteAllText((Join-Path $Artifacts 'source-worktree-status.txt'), ($worktree -join "`n"))
        }
    } catch { Write-Host 'Git metadata unavailable; record the downloaded source archive checksum.' }
}
if (!(Test-Path -LiteralPath (Join-Path $Artifacts 'source-revision.txt'))) {
    [IO.File]::WriteAllText((Join-Path $Artifacts 'source-revision.txt'), 'Git revision unavailable: record the source archive and its checksum.')
}

function Quote-ProcessArgument([string]$Value) {
    # Windows CommandLineToArgvW/CRT quoting; never evaluate arguments in a shell.
    $escaped = [regex]::Replace($Value, '(\\*)"', '$1$1\"')
    $escaped = [regex]::Replace($escaped, '(\\+)$', '$1$1')
    return '"' + $escaped + '"'
}
function Invoke-Editor([string[]]$EditorArguments, [int]$DeadlineSeconds, [string]$OutputLog) {
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $Editor
    $info.Arguments = ($EditorArguments | ForEach-Object { Quote-ProcessArgument $_ }) -join ' '
    $info.WorkingDirectory = $projectDir
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $info
    try {
        if (!$process.Start()) { throw 'Unity process did not start.' }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (!$process.WaitForExit($DeadlineSeconds * 1000)) {
            $process.Kill(); $process.WaitForExit()
            throw "Unity exceeded its $DeadlineSeconds second process deadline. See $Artifacts."
        }
        $output = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
        [IO.File]::WriteAllText($OutputLog, $output)
        if ($process.ExitCode -ne 0) { throw "Unity failed with exit $($process.ExitCode). See $Artifacts." }
    } finally { $process.Dispose() }
}

Invoke-Editor -EditorArguments @('-version') -DeadlineSeconds 60 -OutputLog (Join-Path $Artifacts 'editor-version.txt')
if ((Get-Content -LiteralPath (Join-Path $Artifacts 'editor-version.txt') -Raw) -notmatch '(?m)^6000\.6\.5f1(?:\s|$)') { throw 'The selected Editor is not Unity 6000.6.5f1.' }
$oldMode = $env:ASCENDANCE_TEST_MODE
$oldData = $env:ASCENDANCE_TEST_DATA_DIR
$oldArtExpectedHash = $env:ASCENDANCE_ART_EXPECTED_MANIFEST_SHA256
try {
    $dataDir = Join-Path $Artifacts 'game-data'
    [void][IO.Directory]::CreateDirectory($dataDir)
    [IO.File]::WriteAllText((Join-Path $dataDir '.ascendance-validation-data'), "ascendance-unity-validation-0.10.2h`n")
    $env:ASCENDANCE_TEST_MODE = '1'
    $env:ASCENDANCE_TEST_DATA_DIR = $dataDir
    if ($RequireInstalledArt) { $env:ASCENDANCE_ART_EXPECTED_MANIFEST_SHA256 = $artManifestHash }
    else { $env:ASCENDANCE_ART_EXPECTED_MANIFEST_SHA256 = $null }
    $resultsPath = Join-Path $Artifacts 'validation-results.json'
    # The asynchronous bridge owns Editor exit; -quit would truncate the tests.
    $validationArguments = @('-batchmode','-nographics','-projectPath',$projectDir,
        '-executeMethod','Ascendance.EditorTools.AscendanceBatchValidation0102.Run',
        '-ascendance-results',$resultsPath,'-ascendance-timeout',"$TimeoutSeconds",'-logFile',(Join-Path $Artifacts 'validation.log'))
    if ($RequireInstalledArt) { $validationArguments += '-ascendance-require-installed-art' }
    if ($RequireAllLocationArt) { $validationArguments += '-ascendance-require-all-location-art' }
    Invoke-Editor -EditorArguments $validationArguments `
        -DeadlineSeconds ($TimeoutSeconds + 90) -OutputLog (Join-Path $Artifacts 'editor-console.txt')
    $result = Get-Content -LiteralPath $resultsPath -Raw -Encoding UTF8 | ConvertFrom-Json
    function Test-JsonInteger($Value) { return $Value -is [int] -or $Value -is [long] }
    if ($result.passed -isnot [bool] -or $result.passed -ne $true -or !(Test-JsonInteger $result.errorCount) -or $result.errorCount -ne 0 -or $result.errors -or $result.editorVersion -cne '6000.6.5f1' -or $result.buildVersion -cne '0.10.2h') { throw 'Unity verdict is failed or has unexpected versions/errors.' }
    if ($result.requireInstalledArt -isnot [bool] -or $result.requireAllLocationArt -isnot [bool] -or $result.requireInstalledArt -ne [bool]$RequireInstalledArt -or $result.requireAllLocationArt -ne [bool]$RequireAllLocationArt) {
        throw 'Unity verdict art modes differ from the requested validation.'
    }
    if ($result.artManifestSha256 -isnot [string] -or ($RequireInstalledArt -and ($result.artManifestSha256 -notmatch '^[0-9a-fA-F]{64}$' -or $result.artManifestSha256 -ine $artManifestHash)) -or (!$RequireInstalledArt -and $result.artManifestSha256 -cne '')) {
        throw 'Unity verdict art manifest SHA-256 differs from the required input.'
    }
    $expected = @{ master=54; foundation=24; router=30; network=27; reliability=22; snapshot=20; lobby=32; campaign=42; reaction=30; combatHud=11; villagers=67; locationArt=47 }
    $terminals = @{
        master=@('[ASCENDANCE][0.10.2h TEST]', 'Master Save/Load + Full Regression')
        foundation=@('[ASCENDANCE][0.10.2h SERVER]', 'Dedicated Server Authority Foundation')
        router=@('[ASCENDANCE][0.10.2h ROUTER]', 'Client Command Routing')
        network=@('[ASCENDANCE][0.10.2h NETWORK]', 'Real Network Transport')
        reliability=@('[ASCENDANCE][0.10.2h RELIABILITY]', 'Network Reliability')
        lobby=@('[ASCENDANCE][ONLINE LOBBY]', 'Online Hero Lobby')
        campaign=@('[ASCENDANCE][ONLINE CAMPAIGN]', 'Online Campaign Setup')
        reaction=@('[ASCENDANCE][REACTION AUTHORITY]', 'Reaction Authority')
        combatHud=@('[ASCENDANCE][COMBAT HUD]', 'Combat HUD')
        villagers=@('[ASCENDANCE][Q001 VILLAGERS]', 'Villager gameplay regression')
        locationArt=@('[ASCENDANCE][LOCATION ART]', 'Location Art Framework')
        installedArt=@('[ASCENDANCE][LOCATION ART]', 'Installed Location Art Bundle')
        allLocationArt=@('[ASCENDANCE][LOCATION ART]', 'All Location Art Bindings')
    }
    if ($RequireInstalledArt) { $expected['installedArt'] = 10 }
    if ($RequireAllLocationArt) { $expected['allLocationArt'] = 236 }
    $seen = @{}
    foreach ($gate in $result.gates) {
        if ($expected.Keys -cnotcontains $gate.name -or $seen.ContainsKey($gate.name)) { throw 'Missing, duplicate or unknown Unity runtime gate.' }
        $seen[$gate.name] = $true
        if ($gate.passed -isnot [bool] -or $gate.passed -ne $true -or !(Test-JsonInteger $gate.expectedChecks) -or !(Test-JsonInteger $gate.observedChecks) -or $gate.expectedChecks -ne $expected[$gate.name] -or $gate.observedChecks -ne $expected[$gate.name] -or $gate.terminalMessage -isnot [string]) { throw "Unity gate did not complete: $($gate.name)" }
        if ($gate.name -ceq 'snapshot') { $terminal = "[ASCENDANCE TEST 0.10.2 SNAPSHOT] PASS $([char]0x2014) $($expected[$gate.name]) checks." }
        else { $terminal = "$($terminals[$gate.name][0]) PASS $([char]0x2014) $($terminals[$gate.name][1]) completed: $($expected[$gate.name]) passed, 0 failed." }
        if ($gate.terminalMessage.Trim() -cne $terminal) { throw "Unity gate terminal verdict is invalid: $($gate.name)" }
    }
    if ($seen.Count -ne $expected.Count) { throw 'Unity verdict omitted a required runtime gate.' }
    Write-Host "All $($expected.Count) Unity runtime gates completed successfully."
    function Build-Player([string]$Target, [string]$Method, [string]$Output, [string]$Label) {
        Invoke-Editor -EditorArguments @('-batchmode','-nographics','-quit','-projectPath',$projectDir,'-buildTarget',$Target,
            '-executeMethod',$Method,'-buildOutput',$Output,'-logFile',(Join-Path $Artifacts "build-$Label.log")) `
            -DeadlineSeconds 3600 -OutputLog (Join-Path $Artifacts "build-$Label-console.txt")
        $dataPath = Join-Path ([IO.Path]::GetDirectoryName($Output)) ([IO.Path]::GetFileNameWithoutExtension($Output) + '_Data')
        if (!(Test-Path -LiteralPath $Output -PathType Leaf) -or (Get-Item -LiteralPath $Output).Length -le 0 -or !(Test-Path -LiteralPath $dataPath -PathType Container)) { throw "Unity $Label output is incomplete." }
    }
    if ($BuildLinuxServer) { Build-Player 'Linux64' 'Ascendance.EditorTools.AscendanceDedicatedServerBuilder0100.BuildLinuxDedicatedServer0102' (Join-Path $Artifacts 'LinuxServer/AscendanceServer.x86_64') 'linux-server' }
    if ($BuildWindows) { Build-Player 'Win64' 'Ascendance.EditorTools.AscendanceStandaloneBuilder.BuildWindowsStandalone' (Join-Path $Artifacts 'Windows/Ascendance.exe') 'windows-player' }
} finally {
    $env:ASCENDANCE_TEST_MODE = $oldMode
    $env:ASCENDANCE_TEST_DATA_DIR = $oldData
    $env:ASCENDANCE_ART_EXPECTED_MANIFEST_SHA256 = $oldArtExpectedHash
}
Write-Host "Validation complete. Review logs and builds under $Artifacts."
