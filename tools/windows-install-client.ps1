[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ReleaseDescriptorUrl,
    [Parameter(Mandatory=$true)][string]$ReleaseDescriptorSha256,
    [string]$ExpectedRelease = 'unity-0.10.2h-multiplayer-55d25c0',
    [string]$ExpectedSourceRevision = '55d25c03a3279a8396d5b8954e73180e08f7a0c5',
    [string]$ExpectedRoom = 'ASC-PRIMARY',
    [string]$ExpectedCertificateSha256,
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'

# The seven helpers below are copied verbatim from the previously validated
# owner deployment wrapper. They never request Audit/SACL access or change
# ownership. The client-only flow below never activates or modifies a server.
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

function Get-AscendanceRegularPath {
    param([string]$Path)
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item -isnot [System.IO.FileInfo] -and $item -isnot [System.IO.DirectoryInfo]) {
        throw 'Owner permissions require a regular filesystem file or directory.'
    }
    $component = $item
    while ($null -ne $component) {
        if (($component.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw 'Owner permissions refuse a reparse point in the target or any parent directory.'
        }
        if ($component -is [System.IO.FileInfo]) { $component = $component.Directory }
        else { $component = $component.Parent }
    }
    return $item
}

function Get-AscendanceAccessSecurity {
    param([System.IO.FileSystemInfo]$Item)
    # Never request Audit/SACL: an ordinary Windows user does not have
    # SeSecurityPrivilege, and the existing audit descriptor is not ours to edit.
    $sections = [System.Security.AccessControl.AccessControlSections]::Access -bor
                [System.Security.AccessControl.AccessControlSections]::Owner -bor
                [System.Security.AccessControl.AccessControlSections]::Group
    if ($PSVersionTable.PSEdition -eq 'Core') {
        return [System.IO.FileSystemAclExtensions]::GetAccessControl($Item, $sections)
    }
    return $Item.GetAccessControl($sections)
}

function Protect-AscendanceOwnerPath {
    param([string]$Path)
    $item = Get-AscendanceRegularPath $Path
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $sidType = [System.Security.Principal.SecurityIdentifier]
    $before = Get-AscendanceAccessSecurity $item
    $owner = $before.GetOwner($sidType).Value
    $group = $before.GetGroup($sidType).Value
    if ($owner -ne $identity.User.Value) {
        throw 'This path belongs to another Windows owner. Choose a new output directory owned by your current user; ownership will not be changed.'
    }
    $inheritance = [System.Security.AccessControl.InheritanceFlags]::None
    if ($item -is [System.IO.DirectoryInfo]) {
        $acl = New-Object System.Security.AccessControl.DirectorySecurity
        $inheritance = [System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor
                       [System.Security.AccessControl.InheritanceFlags]::ObjectInherit
    } else {
        $acl = New-Object System.Security.AccessControl.FileSecurity
    }
    # A fresh descriptor has only Access modified. Persisting it through the
    # filesystem ACL API writes DACL_SECURITY_INFORMATION, never owner/group or
    # audit sections. Set-Acl/SetOwner caused SeSecurityPrivilege failures here.
    $acl.SetAccessRuleProtection($true, $false)
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule($identity.User,
        [System.Security.AccessControl.FileSystemRights]::FullControl, $inheritance,
        [System.Security.AccessControl.PropagationFlags]::None,
        [System.Security.AccessControl.AccessControlType]::Allow)
    [void]$acl.AddAccessRule($rule)
    if ($PSVersionTable.PSEdition -eq 'Core') {
        [System.IO.FileSystemAclExtensions]::SetAccessControl($item, $acl)
    } else {
        $item.SetAccessControl($acl)
    }
    $verify = Get-AscendanceAccessSecurity (Get-AscendanceRegularPath $Path)
    $rules = @($verify.GetAccessRules($true, $true, $sidType))
    if (-not $verify.AreAccessRulesProtected -or $rules.Count -ne 1 -or
        $rules[0].IsInherited -or $rules[0].IdentityReference.Value -ne $identity.User.Value -or
        $rules[0].AccessControlType -ne [System.Security.AccessControl.AccessControlType]::Allow -or
        $rules[0].FileSystemRights -ne [System.Security.AccessControl.FileSystemRights]::FullControl -or
        $rules[0].InheritanceFlags -ne $inheritance -or
        $rules[0].PropagationFlags -ne [System.Security.AccessControl.PropagationFlags]::None -or
        $verify.GetOwner($sidType).Value -ne $owner -or $verify.GetGroup($sidType).Value -ne $group) {
        throw 'Owner-only permissions could not be verified. No private invite will be copied to this path.'
    }
}

function New-AscendanceOwnerFile {
    param([string]$Path)
    [void](Get-AscendanceRegularPath (Split-Path -Parent $Path))
    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::CreateNew,
        [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    $stream.Dispose()
    Protect-AscendanceOwnerPath $Path
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


function Assert-AscendanceClientRelease {
    param($Descriptor, [string]$Release, [string]$SourceRevision)
    if ($Release -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$' -or $SourceRevision -notmatch '^[0-9a-f]{40}$') {
        throw 'Missing expected release identity.'
    }
    if (($Descriptor.format -isnot [int] -and $Descriptor.format -isnot [long]) -or $Descriptor.format -ne 1 -or
        $Descriptor.release -isnot [string] -or $Descriptor.release -cne $Release -or
        $Descriptor.sourceRevision -isnot [string] -or $Descriptor.sourceRevision -cne $SourceRevision -or
        $Descriptor.buildVersion -isnot [string] -or $Descriptor.buildVersion -cne '0.10.2h' -or
        $Descriptor.endpoint -isnot [string] -or $Descriptor.endpoint -cne '159.65.100.206:7777') {
        throw 'The descriptor does not identify the reviewed matching h client and endpoint.'
    }
    $expectedArchive = "https://github.com/RubberDuckieGod/ascendance-unity/releases/download/$Release/ascendance-windows-$Release.zip"
    if ($Descriptor.client.archive.url -isnot [string] -or $Descriptor.client.archive.url -cne $expectedArchive -or
        $Descriptor.client.archive.sha256 -isnot [string] -or $Descriptor.client.archive.sha256 -notmatch '^[0-9a-fA-F]{64}$') {
        throw 'The client download differs from the expected release archive.'
    }
}

function Assert-AscendanceClientProvenance {
    param([string]$Path, [string]$SourceRevision)
    try { $proof = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json }
    catch { throw 'The verified client is missing readable build provenance.' }
    if ($proof.schemaVersion -ne 1 -or $proof.outcome -cne 'success' -or $proof.exitCode -ne 0 -or
        $proof.build.target -cne 'StandaloneWindows64' -or $proof.source.vcs -cne 'git' -or
        $proof.source.revision -cne $SourceRevision -or $proof.source.dirty -isnot [bool] -or $proof.source.dirty) {
        throw 'The Windows build does not have the expected clean source provenance.'
    }
}

function Read-AscendanceClientProfile {
    param([string]$Path, [string]$Endpoint, [string]$Room, [string]$CertificateSha256)
    try { $profile = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json }
    catch { throw 'The private connection profile is unreadable. No profile values are displayed.' }
    if (($profile.format -isnot [int] -and $profile.format -isnot [long]) -or $profile.format -ne 1 -or
        $profile.endpoint -isnot [string] -or $profile.endpoint -cne $Endpoint -or
        $profile.room -isnot [string] -or $profile.room -cne $Room -or
        $profile.room -notmatch '^[A-Za-z0-9_-]{1,128}$' -or
        $profile.certificateSha256 -isnot [string] -or $profile.certificateSha256 -notmatch '^[A-Fa-f0-9]{64}$' -or
        $profile.accessKey -isnot [string] -or $profile.accessKey -notmatch '^[A-Fa-f0-9]{64}$') {
        throw 'The private connection profile does not match the expected room, endpoint or secure format.'
    }
    if (-not [string]::IsNullOrWhiteSpace($CertificateSha256) -and
        ($CertificateSha256 -notmatch '^[A-Fa-f0-9]{64}$' -or $profile.certificateSha256 -ine $CertificateSha256)) {
        throw 'The private connection profile does not match the verified activation certificate.'
    }
    return $profile
}

function Assert-AscendanceClientStorage {
    param([string]$Path, [string]$LocalRoot, [string[]]$ExcludedRoots)
    if ([string]::IsNullOrWhiteSpace($LocalRoot)) { throw 'Windows LocalAppData is unavailable.' }
    $local = [IO.Path]::GetFullPath($LocalRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $full = [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if (-not $full.StartsWith($local + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The private client directory must be a fresh directory inside LocalAppData.'
    }
    foreach ($excludedRoot in $ExcludedRoots) {
        if ([string]::IsNullOrWhiteSpace($excludedRoot)) { continue }
        $excluded = [IO.Path]::GetFullPath($excludedRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
        if ($full.Equals($excluded, [StringComparison]::OrdinalIgnoreCase) -or
            $full.StartsWith($excluded + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Private connection files cannot be installed on the Desktop or in OneDrive.'
        }
    }
}

function New-AscendanceClientDirectory {
    param([string]$RequestedDirectory)
    $local = [IO.Path]::GetFullPath([Environment]::GetFolderPath('LocalApplicationData')).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ([string]::IsNullOrWhiteSpace($RequestedDirectory)) {
        $RequestedDirectory = Join-Path $local ('Ascendance\Multiplayer-' + [Guid]::NewGuid().ToString('N'))
    }
    $full = [IO.Path]::GetFullPath($RequestedDirectory)
    $excluded = @([Environment]::GetFolderPath('Desktop'), $env:OneDrive, $env:OneDriveConsumer, $env:OneDriveCommercial)
    Assert-AscendanceClientStorage $full $local $excluded
    if (Test-Path -LiteralPath $full) { throw 'Choose a new client directory; existing files will not be overwritten.' }
    [void](New-Item -ItemType Directory -Path $full)
    Protect-AscendanceOwnerPath $full
    return $full
}

if ($MyInvocation.InvocationName -eq '.') { return }
if ($env:OS -ne 'Windows_NT') { throw 'Run this client installer in normal Windows PowerShell, not the server terminal.' }
if ($ExpectedRoom -notmatch '^[A-Za-z0-9_-]{1,128}$') { throw 'Invalid expected room identity.' }
if (-not [string]::IsNullOrWhiteSpace($ExpectedCertificateSha256) -and $ExpectedCertificateSha256 -notmatch '^[A-Fa-f0-9]{64}$') {
    throw 'Invalid expected activation certificate.'
}
if (-not (Get-Command scp -CommandType Application -ErrorAction SilentlyContinue)) { throw 'Windows OpenSSH scp is required.' }
$identityFile = Join-Path $env:USERPROFILE '.ssh\id_ed25519'
if (-not (Test-Path -LiteralPath $identityFile -PathType Leaf)) { throw 'The existing Windows SSH identity file was not found.' }
$session = New-AscendanceClientDirectory $OutputDirectory
$descriptorFile = Join-Path $session 'release.json'
Get-AscendanceVerifiedFile $ReleaseDescriptorUrl $ReleaseDescriptorSha256 $descriptorFile
try { $descriptor = Get-Content -LiteralPath $descriptorFile -Raw | ConvertFrom-Json }
catch { throw 'The pinned release descriptor is unreadable.' }
Assert-AscendanceClientRelease $descriptor $ExpectedRelease $ExpectedSourceRevision
$clientZip = Join-Path $session 'Ascendance-Windows.zip'
Get-AscendanceVerifiedFile $descriptor.client.archive.url $descriptor.client.archive.sha256 $clientZip
$clientDirectory = Join-Path $session 'Client'
Expand-AscendanceClientZip $clientZip $clientDirectory
$executables = @(Get-ChildItem -LiteralPath $clientDirectory -Filter Ascendance.exe -File -Recurse)
if ($executables.Count -ne 1) { throw 'The Windows archive does not contain exactly one Ascendance.exe.' }
$gameDirectory = $executables[0].DirectoryName
Protect-AscendanceOwnerPath $gameDirectory
foreach ($name in @('Ascendance_Data','UnityPlayer.dll','Play-AscendanceMultiplayer.ps1','Play-AscendanceMultiplayer.cmd','Ascendance.provenance.json')) {
    $item = Get-AscendanceRegularPath (Join-Path $gameDirectory $name)
    if ($name -eq 'Ascendance_Data' -and $item -isnot [IO.DirectoryInfo]) { throw 'The extracted client data directory is invalid.' }
    if ($name -ne 'Ascendance_Data' -and $item -isnot [IO.FileInfo]) { throw 'A required extracted client file is invalid.' }
}
Assert-AscendanceClientProvenance (Join-Path $gameDirectory 'Ascendance.provenance.json') $ExpectedSourceRevision
$privateInvite = Join-Path $gameDirectory 'Ascendance.invite.json'
New-AscendanceOwnerFile $privateInvite
Write-Host 'Copying the private connection profile with your existing Windows SSH key. Enter its passphrase only in this terminal.'
# This is an ordinary interactive SCP invocation. There is no remote command,
# stdin pipe, deployment action or invitation/key value in an argument.
& scp -o IdentitiesOnly=yes -o StrictHostKeyChecking=yes -i $identityFile 'root@159.65.100.206:/etc/ascendance-secure/owner-connection.json' $privateInvite
if ($LASTEXITCODE -ne 0) { throw 'The private profile could not be copied. The server was not changed by this client-only installer.' }
Protect-AscendanceOwnerPath $privateInvite
$privateProfile = Read-AscendanceClientProfile $privateInvite ([string]$descriptor.endpoint) $ExpectedRoom $ExpectedCertificateSha256
$privateProfile = $null
[pscustomobject]@{
    LauncherPath = (Join-Path $gameDirectory 'Play-AscendanceMultiplayer.cmd')
    ClientDirectory = $gameDirectory
    InvitePath = $privateInvite
} | ConvertTo-Json -Depth 3
