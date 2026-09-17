[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$Path,
    [switch]$RemoveBranch
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$resolved = (Resolve-Path -LiteralPath $Path).Path
$entries = (& git -C $repoRoot worktree list --porcelain) -join "`n"
if ($entries -notmatch [regex]::Escape("worktree $($resolved.Replace('\','/'))") -and
    $entries -notmatch [regex]::Escape("worktree $resolved")) {
    throw "Path is not a registered worktree: $resolved"
}

$status = & git -C $resolved status --porcelain
if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect worktree status.' }
if ($status) { throw 'Refusing to remove a dirty worktree.' }

$branch = (& git -C $resolved branch --show-current).Trim()
if (-not $branch) { throw 'Refusing to remove a detached worktree.' }
& git -C $repoRoot merge-base --is-ancestor $branch dev
if ($LASTEXITCODE -ne 0) { throw "Refusing to remove: $branch is not merged into dev." }

if ($PSCmdlet.ShouldProcess($resolved, "Remove clean, merged worktree on $branch")) {
    & git -C $repoRoot worktree remove $resolved
    if ($LASTEXITCODE -ne 0) { throw 'git worktree remove failed.' }
    if ($RemoveBranch) {
        & git -C $repoRoot branch -d $branch
        if ($LASTEXITCODE -ne 0) { throw "Worktree removed, but branch deletion failed: $branch" }
    }
    Write-Output "Removed worktree: $resolved"
}
