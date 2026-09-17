[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

& (Join-Path $PSScriptRoot 'verify-context-budget.ps1')
if (-not $?) { throw 'Context budget verification failed.' }
& (Join-Path $PSScriptRoot 'verify-env.ps1')
if (-not $?) { throw 'Environment verification failed.' }

& git -C $repoRoot diff --check
if ($LASTEXITCODE -ne 0) { throw 'git diff --check failed.' }

Write-Output 'OK: fast harness checks passed. This does not imply game or Unity test coverage.'
