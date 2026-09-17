[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$warnings = 0

function Test-Lines([string]$Path, [int]$Limit) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return }
    $count = (Get-Content -LiteralPath $Path).Count
    if ($count -gt $Limit) {
        Write-Warning "$Path has $count lines (warning threshold: $Limit)."
        $script:warnings++
    }
}

Test-Lines (Join-Path $repoRoot 'AGENTS.md') 120
Get-ChildItem (Join-Path $repoRoot 'docs\domains') -Filter '*.md' -File -ErrorAction SilentlyContinue |
    Where-Object Name -ne 'INDEX.md' | ForEach-Object { Test-Lines $_.FullName 150 }
Get-ChildItem (Join-Path $repoRoot '.agents\skills') -Filter 'SKILL.md' -File -Recurse -ErrorAction SilentlyContinue |
    ForEach-Object { Test-Lines $_.FullName 120 }
Get-ChildItem (Join-Path $repoRoot 'tasks') -Filter 'plan.md' -File -Recurse -ErrorAction SilentlyContinue |
    ForEach-Object { Test-Lines $_.FullName 80 }
Get-ChildItem (Join-Path $repoRoot 'tasks') -Filter 'handoff.md' -File -Recurse -ErrorAction SilentlyContinue |
    ForEach-Object { Test-Lines $_.FullName 30 }

Write-Output "Context budget check complete: $warnings warning(s)."
