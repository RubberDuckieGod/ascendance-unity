[CmdletBinding()]
param(
    [string]$InvitePath,
    [string]$PlayerName,
    [string]$GameExecutable
)
$ErrorActionPreference = 'Stop'

function Read-AscendanceInvite {
    param([Parameter(Mandatory=$true)][string]$Path)
    try { $invite = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json }
    catch { throw "The private invite could not be read or parsed. Obtain a fresh invite from the server owner." }
    if ($invite.format -ne 1) { throw 'This invite format is not supported.' }
    if ([string]$invite.endpoint -notmatch '^([A-Za-z0-9.-]+):([0-9]{1,5})$') { throw 'The invite has an invalid server endpoint.' }
    $portNumber = [int]$Matches[2]
    if ($portNumber -lt 1 -or $portNumber -gt 65535) { throw 'The invite has an invalid server port.' }
    if ([string]$invite.room -notmatch '^[A-Za-z0-9_-]{1,128}$') { throw 'The invite has an invalid room code.' }
    if ([string]$invite.certificateSha256 -notmatch '^[A-Fa-f0-9]{64}$') { throw 'The invite is missing its pinned server certificate.' }
    if ([string]$invite.accessKey -notmatch '^[A-Fa-f0-9]{64}$') { throw 'The invite is missing its private room access key.' }
    return $invite
}

function Get-AscendancePlayerArguments {
    param([Parameter(Mandatory=$true)]$Invite, [Parameter(Mandatory=$true)][string]$Name)
    if ($Name -notmatch '^[A-Za-z0-9][A-Za-z0-9 ._-]{0,39}$') {
        throw 'Use a player name of 1-40 letters, numbers, spaces, dots, underscores or hyphens.'
    }
    return @('-ascendance-connect', [string]$Invite.endpoint,
             '-room', [string]$Invite.room, '-player', ('"' + $Name + '"'),
             '-ascendance-tls', '-ascendance-cert-sha256', [string]$Invite.certificateSha256)
}

# Dot-sourcing exposes only the pure validation helpers for isolated fixture tests.
if ($MyInvocation.InvocationName -eq '.') { return }

if ($env:OS -ne 'Windows_NT') { throw 'This launcher is for the Windows game build.' }
# Windows PowerShell 5.1 can evaluate parameter defaults before PSScriptRoot is
# populated. Resolve adjacent files only after entering the script body.
$launcherDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($launcherDirectory)) {
    $launcherDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
}
if ([string]::IsNullOrWhiteSpace($InvitePath)) {
    $InvitePath = Join-Path $launcherDirectory 'Ascendance.invite.json'
}
if ([string]::IsNullOrWhiteSpace($GameExecutable)) {
    $GameExecutable = Join-Path $launcherDirectory 'Ascendance.exe'
}
if (-not (Test-Path -LiteralPath $InvitePath -PathType Leaf)) {
    $InvitePath = Read-Host 'Full path to the private Ascendance invite JSON from the server owner'
}
$profile = Read-AscendanceInvite -Path $InvitePath
if ([string]::IsNullOrWhiteSpace($PlayerName)) { $PlayerName = Read-Host 'Your player name' }
$launchArguments = Get-AscendancePlayerArguments -Invite $profile -Name $PlayerName
if (-not (Test-Path -LiteralPath $GameExecutable -PathType Leaf)) {
    throw 'Ascendance.exe was not found. Extract the complete Windows game ZIP first.'
}

$previousAccessKey = [Environment]::GetEnvironmentVariable('ASCENDANCE_ROOM_ACCESS_KEY', 'Process')
try {
    # The invite key belongs only in the child's environment, never command-line
    # arguments, public launchers, console output, URLs or server logs.
    [Environment]::SetEnvironmentVariable('ASCENDANCE_ROOM_ACCESS_KEY', [string]$profile.accessKey, 'Process')
    Start-Process -FilePath $GameExecutable -ArgumentList $launchArguments -WorkingDirectory (Split-Path -Parent $GameExecutable) | Out-Null
} finally {
    [Environment]::SetEnvironmentVariable('ASCENDANCE_ROOM_ACCESS_KEY', $previousAccessKey, 'Process')
}
Write-Host 'Ascendance launched with encrypted multiplayer settings.'
Write-Host 'Choose JOIN ONLINE CAMPAIGN, select one available Hero, then JOIN CAMPAIGN.'
Write-Host 'Each player must choose a different Hero in the server party.'
