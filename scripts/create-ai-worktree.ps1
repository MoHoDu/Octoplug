[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][ValidatePattern('^TASK-\d{8}-\d{3}$')][string]$Id,
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9]+(?:-[a-z0-9]+)*$')][string]$Slug,
    [string]$Base = 'dev',
    [string]$WorktreeRoot
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if (-not $WorktreeRoot) {
    $envPath = Join-Path $repoRoot '.env'
    if (Test-Path -LiteralPath $envPath) {
        $line = Get-Content -LiteralPath $envPath | Where-Object { $_ -match '^WORKTREE_ROOT=' } | Select-Object -First 1
        if ($line) { $WorktreeRoot = $line.Substring('WORKTREE_ROOT='.Length).Trim() }
    }
}
if (-not $WorktreeRoot) { $WorktreeRoot = 'D:/github-worktrees/Octoplug' }

$branch = "task/$Id-$Slug"
$path = [System.IO.Path]::GetFullPath((Join-Path $WorktreeRoot "$Id-$Slug"))
$rootPath = [System.IO.Path]::GetFullPath($WorktreeRoot)
if (-not $path.StartsWith($rootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Resolved worktree path escaped WORKTREE_ROOT.'
}
if (Test-Path -LiteralPath $path) { throw "Worktree path already exists: $path" }

git -C $repoRoot show-ref --verify --quiet "refs/heads/$branch"
if ($LASTEXITCODE -eq 0) { throw "Branch already exists: $branch" }
git -C $repoRoot rev-parse --verify $Base 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Base ref not found: $Base" }

if ($PSCmdlet.ShouldProcess($path, "Create Git worktree on $branch from $Base")) {
    if (-not (Test-Path -LiteralPath $rootPath)) {
        New-Item -ItemType Directory -Path $rootPath -Force | Out-Null
    }
    & git -C $repoRoot worktree add -b $branch $path $Base
    if ($LASTEXITCODE -ne 0) { throw 'git worktree add failed.' }
    Write-Output "Created worktree: $path"
    Write-Output 'Open this path as a separate Unity project; never share Library with another worktree.'
}
