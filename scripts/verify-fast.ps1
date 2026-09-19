[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

# General repository/context budget checks.
& (Join-Path $PSScriptRoot 'verify-context-budget.ps1')
if (-not $?) {
    throw 'Context budget verification failed.'
}

# Active Task document budget.
#
# Intentionally NON-STRICT here:
# an oversized task should trigger compaction guidance,
# but should not block normal development verification.
& (Join-Path $PSScriptRoot 'verify-task-context.ps1')
if (-not $?) {
    throw 'Task context verification script failed.'
}

# Local environment / ignored configuration checks.
& (Join-Path $PSScriptRoot 'verify-env.ps1')
if (-not $?) {
    throw 'Environment verification failed.'
}

# Git whitespace / patch sanity.
& git -C $repoRoot diff --check
if ($LASTEXITCODE -ne 0) {
    throw 'git diff --check failed.'
}

Write-Output 'OK: fast harness checks passed. This does not imply game or Unity test coverage.'