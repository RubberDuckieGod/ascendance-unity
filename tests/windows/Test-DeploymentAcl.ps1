[CmdletBinding()]
param(
    [ValidateSet('Supervisor', 'Prepare', 'Test')][string]$Phase = 'Supervisor',
    [string]$WorkRoot,
    [string]$ExpectedSid,
    [string]$WrapperPath,
    [string]$ResultDirectory
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$sidType = [System.Security.Principal.SecurityIdentifier]
$accessSections = [System.Security.AccessControl.AccessControlSections]::Access -bor
    [System.Security.AccessControl.AccessControlSections]::Owner -bor
    [System.Security.AccessControl.AccessControlSections]::Group
$checks = New-Object 'System.Collections.Generic.List[object]'

function Assert-Fixture {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}
function Check-Fixture {
    param([string]$Name, [bool]$Passed)
    $checks.Add([pscustomobject]@{ name = $Name; passed = $Passed })
    Assert-Fixture $Passed $Name
    Write-Host "PASS $Name"
}
function Read-Access {
    param([string]$Path)
    return (Get-Item -LiteralPath $Path -Force).GetAccessControl($accessSections)
}
function Access-Sddl {
    param([string]$Path)
    return (Read-Access $Path).GetSecurityDescriptorSddlForm($accessSections)
}
function Check-OwnerOnly {
    param([string]$Path, [bool]$Directory)
    $acl = Read-Access $Path
    $rules = @($acl.GetAccessRules($true, $true, $sidType))
    $inheritance = [System.Security.AccessControl.InheritanceFlags]::None
    if ($Directory) {
        $inheritance = [System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor
            [System.Security.AccessControl.InheritanceFlags]::ObjectInherit
    }
    Check-Fixture 'DACL is protected and contains only the ordinary user with explicit FullControl' (
        $acl.AreAccessRulesProtected -and $rules.Count -eq 1 -and -not $rules[0].IsInherited -and
        $rules[0].IdentityReference.Value -eq $ExpectedSid -and
        $rules[0].AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow -and
        $rules[0].FileSystemRights -eq [System.Security.AccessControl.FileSystemRights]::FullControl -and
        $rules[0].InheritanceFlags -eq $inheritance -and
        $rules[0].PropagationFlags -eq [System.Security.AccessControl.PropagationFlags]::None)
}
function Assert-Refused {
    param([string]$Path, [string]$Reason)
    $message = ''
    try { Protect-AscendanceOwnerPath $Path } catch { $message = $_.Exception.Message }
    Check-Fixture "Unsafe fixture is refused ($Reason)" ($message -match $Reason)
}

if ($Phase -ne 'Supervisor') {
    $evidence = [ordered]@{ phase = $Phase; checks = $checks; passed = $false }
    try {
        $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
        $principal = New-Object System.Security.Principal.WindowsPrincipal($identity)
        $privileges = @(& "$env:SystemRoot\System32\whoami.exe" /priv /fo csv /nh)
        Assert-Fixture ($LASTEXITCODE -eq 0) 'Cannot inspect the real child token.'
        $evidence.identitySid = $identity.User.Value
        $evidence.powershellVersion = $PSVersionTable.PSVersion.ToString()
        $evidence.privileges = $privileges
        Check-Fixture 'Actual child is Windows PowerShell 5.1' ($PSVersionTable.PSVersion.Major -eq 5 -and $PSVersionTable.PSVersion.Minor -eq 1)
        Check-Fixture 'Actual child identity is the new isolated account' ($identity.User.Value -eq $ExpectedSid)
        Check-Fixture 'Actual child token is not an administrator' (-not $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator))
        Check-Fixture 'Actual child token has no SeSecurityPrivilege, even disabled' (($privileges -join ' ') -notmatch '\bSeSecurityPrivilege\b')
        Check-Fixture 'No GitHub credential is supplied to the child' ([string]::IsNullOrEmpty($env:GH_TOKEN) -and [string]::IsNullOrEmpty($env:GITHUB_TOKEN))
        # Any accidental wrapper-main execution must fail before a network call.
        function global:Invoke-WebRequest { throw 'Network downloads are forbidden in the ACL fixture.' }
        function global:ssh { throw 'SSH is forbidden in the ACL fixture.' }
        function global:scp { throw 'SCP is forbidden in the ACL fixture.' }
        . $WrapperPath -ReleaseDescriptorUrl 'https://github.com/RubberDuckieGod/ascendance-unity/releases/download/fixture/release.json' -ReleaseDescriptorSha256 ('a' * 64)
        $owned = Join-Path $WorkRoot 'ordinary-owned'
        $directory = Join-Path $owned 'directory'
        $file = Join-Path $owned 'file.txt'
        $target = Join-Path $owned 'junction-target'
        if ($Phase -eq 'Prepare') {
            [void](New-Item -ItemType Directory -Path $directory -Force)
            [void](New-Item -ItemType Directory -Path $target)
            [IO.File]::WriteAllText($file, 'preserve these synthetic bytes')
            [IO.File]::WriteAllText((Join-Path $target 'child.txt'), 'junction target must remain unchanged')
            foreach ($path in @($directory, $file)) {
                $acl = Read-Access $path
                Check-Fixture 'Fixture is genuinely owned by the ordinary user' ($acl.GetOwner($sidType).Value -eq $ExpectedSid)
                Check-Fixture 'Fixture initially inherits foreign Everyone access' (@($acl.GetAccessRules($true, $true, $sidType) | Where-Object { $_.IsInherited -and $_.IdentityReference.Value -eq 'S-1-1-0' }).Count -gt 0)
            }
        } else {
            foreach ($path in @($directory, $file)) {
                $before = Read-Access $path
                Protect-AscendanceOwnerPath $path
                Check-OwnerOnly $path (Test-Path -LiteralPath $path -PathType Container)
                $after = Read-Access $path
                Check-Fixture 'Owner and primary group are unchanged' ($before.GetOwner($sidType).Value -eq $after.GetOwner($sidType).Value -and $before.GetGroup($sidType).Value -eq $after.GetGroup($sidType).Value)
                $first = Access-Sddl $path
                Protect-AscendanceOwnerPath $path
                Check-Fixture 'Protect is idempotent' ((Access-Sddl $path) -eq $first)
            }
            Check-Fixture 'Protect preserves file contents' ([IO.File]::ReadAllText($file) -eq 'preserve these synthetic bytes')
            $childDirectory = Join-Path $directory 'new-child'
            [void](New-Item -ItemType Directory -Path $childDirectory)
            $childFile = Join-Path $childDirectory 'inherited.txt'
            [IO.File]::WriteAllText($childFile, 'synthetic child')
            foreach ($path in @($childDirectory, $childFile)) {
                $rules = @((Read-Access $path).GetAccessRules($true, $true, $sidType))
                Check-Fixture 'New descendants inherit only ordinary-user FullControl' ($rules.Count -eq 1 -and $rules[0].IsInherited -and $rules[0].IdentityReference.Value -eq $ExpectedSid -and $rules[0].FileSystemRights -eq [System.Security.AccessControl.FileSystemRights]::FullControl)
            }
            $invite = Join-Path $directory 'synthetic-invite.json'
            New-AscendanceOwnerFile $invite
            Check-OwnerOnly $invite $false
            Check-Fixture 'Invite destination is protected before any bytes are written' ((Get-Item -LiteralPath $invite).Length -eq 0)
            [IO.File]::WriteAllText($invite, '{"fixture":"not-a-real-invite"}')
            Protect-AscendanceOwnerPath $invite
            Check-OwnerOnly $invite $false
            $copy = Join-Path $directory 'synthetic-invite-copy.json'
            New-AscendanceOwnerFile $copy
            Check-OwnerOnly $copy $false
            Copy-Item -LiteralPath $invite -Destination $copy -Force
            Protect-AscendanceOwnerPath $copy
            Check-OwnerOnly $copy $false
            Check-Fixture 'Protected invite overwrite and copy preserve exact bytes' ([IO.File]::ReadAllText($copy) -eq [IO.File]::ReadAllText($invite))
            $foreign = Join-Path $WorkRoot 'foreign-owner.txt'
            $foreignBefore = Access-Sddl $foreign
            Assert-Refused $foreign 'another Windows owner'
            Check-Fixture 'Refused foreign-owner file is unchanged' ((Access-Sddl $foreign) -eq $foreignBefore)
            $targetBefore = Access-Sddl $target
            $link = Join-Path $WorkRoot 'junction'
            Assert-Refused $link 'reparse point'
            Assert-Refused (Join-Path $link 'child.txt') 'reparse point'
            Check-Fixture 'Refused junction and ancestor leave their target unchanged' ((Access-Sddl $target) -eq $targetBefore)
        }
        $evidence.passed = $true
    } catch {
        $evidence.error = $_.Exception.Message
        throw
    } finally {
        if ($Phase -eq 'Test') {
            # Only after the private-DACL assertions, let the supervisor inspect
            # the seeded SACL and delete this synthetic tree. The ordinary owner
            # still lacks SeSecurityPrivilege and cannot restore/change a SACL.
            try {
                $ownedRoot = Join-Path $WorkRoot 'ordinary-owned'
                $items = @((Get-Item -LiteralPath $ownedRoot)) + @(Get-ChildItem -LiteralPath $ownedRoot -Recurse -Force)
                $administrators = New-Object Security.Principal.SecurityIdentifier('S-1-5-32-544')
                foreach ($item in $items) {
                    $acl = $item.GetAccessControl($accessSections)
                    $inheritance = [Security.AccessControl.InheritanceFlags]::None
                    if ($item -is [IO.DirectoryInfo]) {
                        $inheritance = [Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit
                    }
                    $rule = New-Object Security.AccessControl.FileSystemAccessRule($administrators, 'FullControl', $inheritance, 'None', 'Allow')
                    [void]$acl.AddAccessRule($rule)
                    $item.SetAccessControl($acl)
                }
                $evidence.supervisorAccess = 'Restored after ordinary-user assertions, DACL only'
            } catch {
                $evidence.passed = $false
                $evidence.cleanupAccessError = $_.Exception.Message
            }
        }
        $evidence | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $WorkRoot "$Phase-result.json") -Encoding UTF8
        Assert-Fixture $evidence.passed 'Ordinary-user validation or synthetic-fixture cleanup access failed.'
    }
    return
}

Assert-Fixture ($env:OS -eq 'Windows_NT') 'This fixture requires a real Windows runner.'
Assert-Fixture (-not [string]::IsNullOrWhiteSpace($ResultDirectory)) 'A result directory is required.'
[void](New-Item -ItemType Directory -Path $ResultDirectory -Force)
$principal = New-Object System.Security.Principal.WindowsPrincipal([System.Security.Principal.WindowsIdentity]::GetCurrent())
Assert-Fixture ($principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)) 'Supervisor needs administrator rights only to create the isolated nonadmin test account.'
$root = Join-Path $env:SystemRoot ("Temp\Ascendance-Acl-" + [Guid]::NewGuid().ToString('N'))
$username = 'ascacl' + [Guid]::NewGuid().ToString('N').Substring(0, 10)
$account = $null
$summary = [ordered]@{ passed = $false; scope = 'Actual Windows PowerShell 5.1, separate nonadmin local-user token; no game, deployment, SSH, or download'; audit = @{ status = 'NOT_RUN' } }
try {
    [void](New-Item -ItemType Directory -Path $root)
    $password = ConvertTo-SecureString ('Aa!' + [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')) -AsPlainText -Force
    $account = New-LocalUser -Name $username -Password $password -AccountNeverExpires -PasswordNeverExpires -Description 'Disposable Ascendance ACL CI fixture'
    if (-not @(Get-LocalGroupMember -SID 'S-1-5-32-545' | Where-Object { $_.SID.Value -eq $account.SID.Value }).Count) {
        Add-LocalGroupMember -SID 'S-1-5-32-545' -Member $account
    }
    $ExpectedSid = $account.SID.Value
    $credential = New-Object Management.Automation.PSCredential("$env:COMPUTERNAME\$username", $password)
    $rootAcl = (Get-Item -LiteralPath $root).GetAccessControl($accessSections)
    $rootAcl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($account.SID, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
    $rootAcl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule((New-Object Security.Principal.SecurityIdentifier('S-1-1-0')), 'ReadAndExecute', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
    (Get-Item -LiteralPath $root).SetAccessControl($rootAcl)
    $fixtureScript = Join-Path $root 'fixture.ps1'
    $WrapperPath = Join-Path $root 'wrapper.ps1'
    Copy-Item -LiteralPath $PSCommandPath -Destination $fixtureScript
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\..\tools\windows-deploy-multiplayer.ps1') -Destination $WrapperPath
    [IO.File]::WriteAllText((Join-Path $root 'foreign-owner.txt'), 'foreign synthetic fixture')
    foreach ($childPhase in @('Prepare', 'Test')) {
        if ($childPhase -eq 'Test') {
            [void](New-Item -ItemType Junction -Path (Join-Path $root 'junction') -Target (Join-Path $root 'ordinary-owned\junction-target'))
            $auditBefore = @{}
            $auditItems = @{}
            try {
                foreach ($path in @((Join-Path $root 'ordinary-owned\directory'), (Join-Path $root 'ordinary-owned\file.txt'))) {
                    $audit = Get-Acl -LiteralPath $path -Audit
                    $rule = New-Object Security.AccessControl.FileSystemAuditRule((New-Object Security.Principal.SecurityIdentifier('S-1-1-0')), 'ReadData', 'Success')
                    [void]$audit.AddAuditRule($rule)
                    $item = Get-Item -LiteralPath $path
                    $item.SetAccessControl($audit)
                    $auditItems[$path] = $item
                    $auditBefore[$path] = $item.GetAccessControl([Security.AccessControl.AccessControlSections]::Audit).GetSecurityDescriptorSddlForm([Security.AccessControl.AccessControlSections]::Audit)
                }
                $summary.audit = @{ status = 'PREPARED'; fixtures = $auditBefore.Count }
            } catch {
                $summary.audit = @{ status = 'UNSUPPORTED'; reason = $_.Exception.Message }
                Write-Host 'SACL fixture explicitly unsupported on this runner; core nonadmin DACL checks remain required.'
            }
        }
        $arguments = @('-NoLogo', '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', $fixtureScript, '-Phase', $childPhase, '-WorkRoot', $root, '-ExpectedSid', $ExpectedSid, '-WrapperPath', $WrapperPath)
        $process = Start-Process -FilePath "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -ArgumentList $arguments -Credential $credential -WorkingDirectory $root -PassThru -Wait -RedirectStandardOutput (Join-Path $root "$childPhase-stdout.txt") -RedirectStandardError (Join-Path $root "$childPhase-stderr.txt")
        $resultFile = Join-Path $root "$childPhase-result.json"
        if (Test-Path -LiteralPath $resultFile) { Copy-Item -LiteralPath $resultFile -Destination $ResultDirectory }
        Get-Content -LiteralPath (Join-Path $root "$childPhase-stdout.txt")
        if ($process.ExitCode -ne 0) {
            Get-Content -LiteralPath (Join-Path $root "$childPhase-stderr.txt")
            throw "$childPhase ordinary-user process failed with exit $($process.ExitCode)."
        }
    }
    if ($summary.audit.status -eq 'PREPARED') {
        foreach ($path in $auditBefore.Keys) {
            $after = $auditItems[$path].GetAccessControl([Security.AccessControl.AccessControlSections]::Audit).GetSecurityDescriptorSddlForm([Security.AccessControl.AccessControlSections]::Audit)
            Assert-Fixture ($after -eq $auditBefore[$path]) 'Existing SACL changed during ordinary-user DACL protection.'
        }
        $summary.audit.status = 'PASS'
    }
    $summary.passed = $true
} catch {
    $summary.error = $_.Exception.Message
    $summary.errorStack = $_.ScriptStackTrace
    throw
} finally {
    try {
        if (Test-Path -LiteralPath (Join-Path $root 'junction')) { [IO.Directory]::Delete((Join-Path $root 'junction')) }
        if (Test-Path -LiteralPath $root) {
            & "$env:SystemRoot\System32\takeown.exe" /F $root /R /D Y /A | Out-Null
            & "$env:SystemRoot\System32\icacls.exe" $root /grant '*S-1-5-32-544:(OI)(CI)F' /T /C | Out-Null
            Remove-Item -LiteralPath $root -Recurse -Force
        }
        if ($null -ne $account) {
            $profile = Get-CimInstance Win32_UserProfile | Where-Object { $_.SID -eq $account.SID.Value }
            if ($profile) { $profile | Remove-CimInstance }
            Remove-LocalUser -SID $account.SID
        }
        $summary.cleanup = 'PASS'
    } catch {
        $summary.cleanup = 'FAIL'
        $summary.cleanupError = $_.Exception.Message
        $summary.cleanupStack = $_.ScriptStackTrace
        $summary.passed = $false
    }
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $ResultDirectory 'summary.json') -Encoding UTF8
    Assert-Fixture $summary.passed 'Windows ACL validation or cleanup failed; inspect the result artifact.'
}
