[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ReleaseDescriptorUrl,
    [Parameter(Mandatory=$true)][string]$ReleaseDescriptorSha256,
    [string]$SshTarget = 'root@159.65.100.206',
    [string]$OutputDirectory,
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'

function Assert-AscendanceDownloadUrl {
    param([string]$Value)
    $uri = [Uri]$Value
    if ($uri.Scheme -ne 'https' -or $uri.UserInfo -or $uri.Fragment) { throw 'Release downloads require HTTPS without embedded credentials.' }
    if ($uri.Host -eq 'github.com') {
        if ($uri.AbsolutePath -notmatch '^/RubberDuckieGod/ascendance-unity/releases/download/') { throw 'Unexpected release repository.' }
    } elseif ($uri.Host -eq 'raw.githubusercontent.com') {
        if ($uri.AbsolutePath -notmatch '^/RubberDuckieGod/ascendance-unity/[0-9a-f]{40}/') { throw 'Source downloads must be pinned to a full source commit.' }
    } else { throw 'Unexpected release download host.' }
}

function Get-AscendanceVerifiedFile {
    param([string]$Url, [string]$Sha256, [string]$Destination)
    Assert-AscendanceDownloadUrl $Url
    if ($Sha256 -notmatch '^[0-9a-fA-F]{64}$') { throw 'Missing trusted SHA-256 for a release download.' }
    Invoke-WebRequest -Uri $Url -OutFile $Destination -UseBasicParsing
    if ((Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash -ne $Sha256) {
        throw 'Downloaded content does not match the reviewed release checksum.'
    }
}

function Protect-AscendanceOwnerPath {
    param([string]$Path)
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $acl = Get-Acl -LiteralPath $Path
    $acl.SetAccessRuleProtection($true, $false)
    foreach ($rule in @($acl.Access)) { [void]$acl.RemoveAccessRuleAll($rule) }
    if (Test-Path -LiteralPath $Path -PathType Container) {
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule($identity.User, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
    } else {
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule($identity.User, 'FullControl', 'Allow')
    }
    [void]$acl.AddAccessRule($rule)
    $acl.SetOwner($identity.User)
    Set-Acl -LiteralPath $Path -AclObject $acl
}

function Expand-AscendanceClientZip {
    param([string]$Archive, [string]$Destination)
    if (Test-Path -LiteralPath $Destination) { throw 'Client extraction refuses an existing destination.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($Archive)
    try {
        $base = [IO.Path]::GetFullPath($Destination).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        foreach ($entry in $zip.Entries) {
            $name = $entry.FullName
            if ($name.Contains(':') -or $name -match '^[\\/]' -or (($name -split '[\\/]') -contains '..')) { throw 'Unsafe client archive entry.' }
            $mode = ($entry.ExternalAttributes -shr 16) -band 61440
            if ($mode -notin @(0,16384,32768)) { throw 'Client archive contains a link or special file.' }
            $resolved = [IO.Path]::GetFullPath((Join-Path $Destination $name))
            if (-not $resolved.StartsWith($base, [StringComparison]::OrdinalIgnoreCase)) { throw 'Client archive escapes its destination.' }
        }
    } finally { $zip.Dispose() }
    [IO.Compression.ZipFile]::ExtractToDirectory($Archive, $Destination)
}

if ($MyInvocation.InvocationName -eq '.') { return }
if ($env:OS -ne 'Windows_NT') { throw 'Run this owner deployment wrapper from Windows PowerShell.' }
if ($SshTarget -notmatch '^[A-Za-z_][A-Za-z0-9_-]*@[A-Za-z0-9.-]+$') { throw 'Invalid SSH target.' }
foreach ($name in @('ssh','scp')) { if (-not (Get-Command $name -ErrorAction SilentlyContinue)) { throw "Windows OpenSSH $name is required." } }
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Ascendance\Multiplayer'
}
[void](New-Item -ItemType Directory -Path $OutputDirectory -Force)
Protect-AscendanceOwnerPath $OutputDirectory
$session = Join-Path $OutputDirectory ([Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $session)
Protect-AscendanceOwnerPath $session
$descriptorFile = Join-Path $session 'release.json'
Get-AscendanceVerifiedFile $ReleaseDescriptorUrl $ReleaseDescriptorSha256 $descriptorFile
$descriptor = Get-Content -LiteralPath $descriptorFile -Raw | ConvertFrom-Json
if ($descriptor.format -ne 1 -or [string]$descriptor.release -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$' -or [string]$descriptor.sourceRevision -notmatch '^[0-9a-f]{40}$') { throw 'Invalid release descriptor.' }

# Prepare the complete playable client before any remote service activation.
$clientZip = Join-Path $session 'Ascendance-Windows.zip'
Get-AscendanceVerifiedFile $descriptor.client.archive.url $descriptor.client.archive.sha256 $clientZip
$clientDirectory = Join-Path $session 'Client'
Expand-AscendanceClientZip $clientZip $clientDirectory
$executables = @(Get-ChildItem -LiteralPath $clientDirectory -Filter Ascendance.exe -File -Recurse)
if ($executables.Count -ne 1) { throw 'The Windows archive does not contain exactly one Ascendance.exe.' }
$gameDirectory = $executables[0].DirectoryName
foreach ($name in @('Ascendance_Data','UnityPlayer.dll','Play-AscendanceMultiplayer.ps1','Play-AscendanceMultiplayer.cmd')) {
    if (-not (Test-Path -LiteralPath (Join-Path $gameDirectory $name))) { throw "The extracted client is missing $name." }
}

# Download/verify again on the Droplet, rather than uploading private keys or
# adding cloud workspace access. Only the owner's existing SSH credentials run.
# The bootstrap itself is fixed here; every fetched executable script is pinned
# by the already-reviewed descriptor and a separate SHA-256.
$bootstrap = @'
import hashlib,json,os,pathlib,re,subprocess,sys,tempfile,urllib.parse,urllib.request
url,expected,apply=sys.argv[1:4]
def require(ok,message):
 if not ok:raise RuntimeError(message)
def get(url,expected,destination):
 u=urllib.parse.urlsplit(url)
 require(u.scheme=='https' and not u.username and not u.password and not u.fragment,'Unsafe download URL')
 require((u.hostname=='github.com' and u.path.startswith('/RubberDuckieGod/ascendance-unity/releases/download/')) or (u.hostname=='raw.githubusercontent.com' and re.match(r'^/RubberDuckieGod/ascendance-unity/[0-9a-f]{40}/',u.path)),'Unexpected download repository')
 require(re.fullmatch(r'[0-9a-fA-F]{64}',expected) is not None,'Missing trusted checksum')
 with urllib.request.urlopen(url,timeout=120) as response:data=response.read(256*1024*1024+1)
 require(len(data)<=256*1024*1024,'Download exceeds limit')
 require(hashlib.sha256(data).hexdigest()==expected.lower(),'Download checksum mismatch')
 destination.write_bytes(data)
os.umask(0o077)
root=pathlib.Path(tempfile.mkdtemp(prefix='ascendance-reviewed-upload-'))
get(url,expected,root/'release.json');d=json.loads((root/'release.json').read_text())
require(d.get('format')==1 and re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9._-]{0,63}',d.get('release','')) is not None,'Invalid release descriptor')
required=['upgrade-existing.py','inspect-existing.py','release.py','native_server_acceptance.py']
for name in required:
 item=d['helpers'][name];get(item['url'],item['sha256'],root/name)
for name,entry in [('server.zip',d['server']['archive']),('server-manifest.json',d['server']['manifest'])]:get(entry['url'],entry['sha256'],root/name)
command=['python3',str(root/'upgrade-existing.py'),'--descriptor',str(root/'release.json'),'--archive',str(root/'server.zip'),'--manifest',str(root/'server-manifest.json')]
if apply=='yes':command.append('--apply')
result=subprocess.run(command,check=False)
raise SystemExit(result.returncode)
'@

# Arguments have already been restricted to fixed HTTPS URLs/checksums. Single
# quotes are rejected here so the remote shell never interprets URL content.
if ($ReleaseDescriptorUrl.Contains("'") -or $ReleaseDescriptorUrl -match '[\r\n\s]') { throw 'Unsafe descriptor URL for remote bootstrap.' }
$applyValue = if ($Apply) { 'yes' } else { 'no' }
$remoteCommand = "python3 - '$ReleaseDescriptorUrl' '$ReleaseDescriptorSha256' '$applyValue'"
Write-Host 'Connecting using your existing Windows SSH key. Enter its passphrase locally if prompted.'
$publicJson = $bootstrap | & ssh -T -o StrictHostKeyChecking=yes -o ServerAliveInterval=20 -o ServerAliveCountMax=6 $SshTarget $remoteCommand
if ($LASTEXITCODE -ne 0) { throw 'Deployment/preflight did not complete. Read the error above; no success is assumed.' }
$result = ($publicJson -join "`n") | ConvertFrom-Json
if (-not $Apply) {
    $result | ConvertTo-Json -Depth 12
    Write-Host 'Preflight only: no production service activation was requested.'
    return
}
if ($result.status -ne 'ACTIVATED_SECURE') { throw 'The server did not report verified secure activation.' }
$publicResultFile = Join-Path $session 'deployment-result.json'
$result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $publicResultFile -Encoding UTF8
Protect-AscendanceOwnerPath $publicResultFile
if ([string]$result.ownerProfilePath -notmatch '^/etc/ascendance-secure/[A-Za-z0-9._-]+\.json$') { throw 'Unexpected private invite path.' }
$privateInvite = Join-Path $session 'Ascendance.invite.json'
& scp -o StrictHostKeyChecking=yes -o ServerAliveInterval=20 -o ServerAliveCountMax=6 "${SshTarget}:$($result.ownerProfilePath)" $privateInvite
if ($LASTEXITCODE -ne 0) { throw "The server activated, but its private invite could not be retrieved. It remains private on the server. Public activation metadata is saved at $publicResultFile." }
Protect-AscendanceOwnerPath $privateInvite

Copy-Item -LiteralPath $privateInvite -Destination (Join-Path $gameDirectory 'Ascendance.invite.json')
Protect-AscendanceOwnerPath (Join-Path $gameDirectory 'Ascendance.invite.json')
Write-Host "Secure server activated: $($result.endpoint), room $($result.room)."
Write-Host "Server certificate SHA-256: $($result.certificateSha256)"
Write-Host "On-server backup retained at: $($result.backupDirectory)"
Write-Host "Play launcher: $(Join-Path $gameDirectory 'Play-AscendanceMultiplayer.cmd')"
Write-Host "Private invite: $privateInvite"
Write-Host 'Share the game ZIP/download link and the private invite with your friends privately. Do not upload the invite to GitHub or chat.'
