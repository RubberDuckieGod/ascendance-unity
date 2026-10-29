[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$ResultDirectory)
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'This fixture requires a real Windows runner.' }

# Run the previously validated ordinary-user supervisor unchanged except for
# its source-tool copy. This exercises the new client-only tool's actual ACL
# helpers under the same nonadministrator token and existing SACL assertions.
$fixture = Join-Path $PSScriptRoot 'Test-DeploymentAcl.ps1'
$clientTool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\tools\windows-install-client.ps1'))
$source = [IO.File]::ReadAllText($fixture)
$originalCopy = "Copy-Item -LiteralPath (Join-Path `$PSScriptRoot '..\..\tools\windows-deploy-multiplayer.ps1') -Destination `$WrapperPath"
if ([regex]::Matches($source, [regex]::Escape($originalCopy)).Count -ne 1) {
    throw 'The shared supervisor changed; review its single tool-copy substitution before running.'
}
$quotedClientTool = $clientTool.Replace("'", "''")
$replacementCopy = "Copy-Item -LiteralPath '$quotedClientTool' -Destination `$WrapperPath"
$clientFixture = $source.Replace($originalCopy, $replacementCopy)
$temporaryScript = Join-Path ([IO.Path]::GetTempPath()) ('Ascendance-ClientOnlyAcl-' + [Guid]::NewGuid().ToString('N') + '.ps1')
try {
    [IO.File]::WriteAllText($temporaryScript, $clientFixture, [Text.UTF8Encoding]::new($true))
    & $temporaryScript -ResultDirectory $ResultDirectory
} finally {
    if (Test-Path -LiteralPath $temporaryScript) { Remove-Item -LiteralPath $temporaryScript -Force }
}
