[CmdletBinding()]
param(
    [string]$ResultDirectory,
    [string]$LauncherPath,
    [switch]$ObserveBaseline
)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -ne 5 -or $PSVersionTable.PSVersion.Minor -ne 1) {
    throw 'This regression must execute under actual Windows PowerShell 5.1.'
}
if ([string]::IsNullOrWhiteSpace($ResultDirectory)) { $ResultDirectory = Join-Path $env:TEMP ('Ascendance launcher evidence ' + [Guid]::NewGuid().ToString('N')) }
if ([string]::IsNullOrWhiteSpace($LauncherPath)) { $LauncherPath = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'tools\Play-AscendanceMultiplayer.ps1' }
New-Item -ItemType Directory -Path $ResultDirectory -Force | Out-Null
$checks = New-Object 'System.Collections.Generic.List[object]'
$summary = [ordered]@{ format = 'ascendance-windows-launcher-1'; powershellVersion = $PSVersionTable.PSVersion.ToString(); baselineObservation = [bool]$ObserveBaseline; passed = 0; failed = 0; scope = 'Synthetic invitation and executable; no game, server, credentials, deployment or network'; knownLimitations = @('Windows PowerShell 5.1 Start-Process rejects wildcard-bracket characters in the working-directory path. This regression tests paths with spaces, matching the owner installation. Bracket-path failure was observed in workflow 40807326108 and is not marked passing.'); checks = $checks }
function Check-Launcher([string]$Name, [bool]$Passed) {
    $checks.Add([ordered]@{ name = $Name; passed = $Passed })
    if ($Passed) { $summary.passed++ } else { $summary.failed++ }
}
$fixtureKey = ('b' * 64)
$fixturePin = ('a' * 64)
$fixture = Join-Path $ResultDirectory 'Client with spaces'
New-Item -ItemType Directory -Path $fixture | Out-Null
$script = Join-Path $fixture 'Play-AscendanceMultiplayer.ps1'
Copy-Item -LiteralPath $LauncherPath -Destination $script
Copy-Item -LiteralPath (Join-Path (Split-Path -Parent $LauncherPath) 'Play-AscendanceMultiplayer.cmd') -Destination (Join-Path $fixture 'Play-AscendanceMultiplayer.cmd')
@{ format = 1; endpoint = '127.0.0.1:7777'; room = 'FIXTURE'; certificateSha256 = $fixturePin; accessKey = $fixtureKey } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $fixture 'Ascendance.invite.json') -Encoding UTF8
Set-Content -LiteralPath (Join-Path $fixture 'Ascendance.exe') -Value 'Not an executable; mocked in direct invocation.'
$originalKey = [Environment]::GetEnvironmentVariable('ASCENDANCE_ROOM_ACCESS_KEY', 'Process')
try {
    # Execute the normal script body, never dot-source its early-return helper path.
    $mockCases = if ($ObserveBaseline) { @() } else { @($false, $true) }
    foreach ($throwOnLaunch in $mockCases) {
        $global:AscendanceLauncherFixtureCapture = $null
        $global:AscendanceLauncherFixtureThrow = $throwOnLaunch
        function global:Start-Process {
            param($FilePath, $ArgumentList, $WorkingDirectory)
            $global:AscendanceLauncherFixtureCapture = @{ file = $FilePath; arguments = @($ArgumentList); directory = $WorkingDirectory; childKeyCorrect = ([Environment]::GetEnvironmentVariable('ASCENDANCE_ROOM_ACCESS_KEY', 'Process') -eq $fixtureKey) }
            if ($global:AscendanceLauncherFixtureThrow) { throw 'Synthetic launch failure.' }
        }
        $sentinel = 'Parent environment sentinel'
        [Environment]::SetEnvironmentVariable('ASCENDANCE_ROOM_ACCESS_KEY', $sentinel, 'Process')
        $exception = $null
        try { & $script -PlayerName 'Owner Player' | Out-Null } catch { $exception = $_ }
        $label = if ($throwOnLaunch) { 'failed launch' } else { 'successful launch' }
        Check-Launcher ($label + ': reached Start-Process with default sibling paths') ($null -ne $global:AscendanceLauncherFixtureCapture)
        Check-Launcher ($label + ': parent access-key environment restored') ([Environment]::GetEnvironmentVariable('ASCENDANCE_ROOM_ACCESS_KEY', 'Process') -eq $sentinel)
        Check-Launcher ($label + ': expected success/failure') (($throwOnLaunch -and $null -ne $exception -and $exception.Exception.Message -eq 'Synthetic launch failure.') -or (-not $throwOnLaunch -and $null -eq $exception))
        if ($null -ne $global:AscendanceLauncherFixtureCapture) {
            Check-Launcher ($label + ': exact executable and working directory') ($global:AscendanceLauncherFixtureCapture.file -eq (Join-Path $fixture 'Ascendance.exe') -and $global:AscendanceLauncherFixtureCapture.directory -eq $fixture)
            Check-Launcher ($label + ': invitation key only in launch environment') ($global:AscendanceLauncherFixtureCapture.childKeyCorrect -and (@($global:AscendanceLauncherFixtureCapture.arguments | Where-Object { $_ -eq $fixtureKey }).Count -eq 0))
            Check-Launcher ($label + ': TLS pin endpoint room and spaced player arguments') (($global:AscendanceLauncherFixtureCapture.arguments -join '|') -eq ('-ascendance-connect|127.0.0.1:7777|-room|FIXTURE|-player|"Owner Player"|-ascendance-tls|-ascendance-cert-sha256|' + $fixturePin))
        }
        Remove-Item Function:\Start-Process
    }
    if (Test-Path Function:\Start-Process) { Remove-Item Function:\Start-Process }
    if ($true) {
        # An actual .cmd -> powershell.exe -File -> synthetic native child tests
        # Windows argument quoting, defaults and child environment inheritance.
        Remove-Item -LiteralPath (Join-Path $fixture 'Ascendance.exe')
        $exe = Join-Path $fixture 'Ascendance.exe'
        $stub = @'
using System;
using System.IO;
using System.Text;
public class LauncherFixture {
    public static int Main(string[] args) {
        string output = Environment.GetEnvironmentVariable("ASCENDANCE_LAUNCHER_TEST_OUTPUT");
        using (var writer = new StreamWriter(output, false, new UTF8Encoding(false))) {
            writer.WriteLine(Environment.GetEnvironmentVariable("ASCENDANCE_ROOM_ACCESS_KEY") == new string('b', 64) ? "KEY_OK" : "KEY_WRONG");
            writer.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(Environment.CurrentDirectory)));
            foreach (string value in args) writer.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(value)));
        }
        return 0;
    }
}
'@
        Add-Type -TypeDefinition $stub -OutputAssembly (Join-Path $ResultDirectory 'fixture-child.exe') -OutputType ConsoleApplication
        Copy-Item -LiteralPath (Join-Path $ResultDirectory 'fixture-child.exe') -Destination $exe
        $childReport = Join-Path $ResultDirectory 'native-child.txt'
        $sentinel = 'Parent environment sentinel'
        [Environment]::SetEnvironmentVariable('ASCENDANCE_ROOM_ACCESS_KEY', $sentinel, 'Process')
        $oldOutput = [Environment]::GetEnvironmentVariable('ASCENDANCE_LAUNCHER_TEST_OUTPUT', 'Process')
        [Environment]::SetEnvironmentVariable('ASCENDANCE_LAUNCHER_TEST_OUTPUT', $childReport, 'Process')
        try {
            $driver = Join-Path $ResultDirectory 'driver.cmd'
            ('@echo off' + "`r`n" + 'call "' + (Join-Path $fixture 'Play-AscendanceMultiplayer.cmd') + '" -PlayerName "Owner Player"' + "`r`n" + 'exit /b %errorlevel%') | Set-Content -LiteralPath $driver -Encoding Ascii
            $inputFile = Join-Path $ResultDirectory 'empty-input.txt'
            [IO.File]::WriteAllText($inputFile, '')
            $stdout = Join-Path $ResultDirectory 'cmd-stdout.txt'
            $stderr = Join-Path $ResultDirectory 'cmd-stderr.txt'
            $process = Start-Process -FilePath $env:ComSpec -ArgumentList @('/d', '/c', ('"' + $driver + '"')) -WorkingDirectory $env:TEMP -RedirectStandardInput $inputFile -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru -Wait
            if ($ObserveBaseline) {
                $baselineText = [IO.File]::ReadAllText($stdout) + [IO.File]::ReadAllText($stderr)
                Check-Launcher 'Original actual .cmd fails with exact empty-root binding error' ($baselineText.Contains('ParameterArgumentValidationErrorEmptyStringNotAllowed'))
                Check-Launcher 'Original actual .cmd never launches native child' (-not (Test-Path -LiteralPath $childReport))
                $summary['baselineErrorId'] = if ($baselineText.Contains('ParameterArgumentValidationErrorEmptyStringNotAllowed')) { 'ParameterArgumentValidationErrorEmptyStringNotAllowed' } else { 'NOT_REPRODUCED' }
            } else {
            Check-Launcher 'Actual .cmd entrypoint exits successfully' ($process.ExitCode -eq 0)
            for ($poll = 0; $poll -lt 50; $poll++) {
                if ((Test-Path -LiteralPath $childReport) -and @(Get-Content -LiteralPath $childReport).Count -eq 11) { break }
                Start-Sleep -Milliseconds 200
            }
            Check-Launcher 'Actual .cmd launches native fixture' (Test-Path -LiteralPath $childReport)
            if (Test-Path -LiteralPath $childReport) {
                $lines = @(Get-Content -LiteralPath $childReport)
                Check-Launcher 'Actual native child inherited private key' ($lines[0] -eq 'KEY_OK')
                $decoded = @($lines | Select-Object -Skip 1 | ForEach-Object { [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($_)) })
                Check-Launcher 'Actual child working directory preserves spaces' ($decoded[0] -eq $fixture)
                Check-Launcher 'Actual child argv preserves player name TLS pin endpoint and room' (($decoded[1..($decoded.Length - 1)] -join '|') -eq ('-ascendance-connect|127.0.0.1:7777|-room|FIXTURE|-player|Owner Player|-ascendance-tls|-ascendance-cert-sha256|' + $fixturePin))
                Check-Launcher 'Native child receives no access key in argv' (-not (($decoded -join '|').Contains($fixtureKey)))
            }
            }
            Check-Launcher 'Outer parent environment unchanged after actual .cmd' ([Environment]::GetEnvironmentVariable('ASCENDANCE_ROOM_ACCESS_KEY', 'Process') -eq $sentinel)
            Check-Launcher 'Launcher output does not expose synthetic access key' (-not (([IO.File]::ReadAllText($stdout) + [IO.File]::ReadAllText($stderr)).Contains($fixtureKey)))
        } finally {
            [Environment]::SetEnvironmentVariable('ASCENDANCE_LAUNCHER_TEST_OUTPUT', $oldOutput, 'Process')
        }
    }
} finally {
    if (Test-Path Function:\Start-Process) { Remove-Item Function:\Start-Process }
    [Environment]::SetEnvironmentVariable('ASCENDANCE_ROOM_ACCESS_KEY', $originalKey, 'Process')
    Remove-Variable AscendanceLauncherFixtureCapture,AscendanceLauncherFixtureThrow -Scope Global -ErrorAction SilentlyContinue
    # Exclude synthetic invite and binaries from uploaded evidence.
    Remove-Item -LiteralPath $fixture -Recurse -Force
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $ResultDirectory 'result.json') -Encoding UTF8
}
if ($summary.failed -gt 0) { throw "Launcher regression failed: $($summary.failed) assertions; see result.json." }
Write-Host "Launcher regression passed: $($summary.passed) assertions on Windows PowerShell $($PSVersionTable.PSVersion)."
