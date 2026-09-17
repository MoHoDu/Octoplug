[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArgs
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$unity = Get-Command unity -ErrorAction Stop

& $unity.Source mcp --project-path $repoRoot @RemainingArgs
exit $LASTEXITCODE
