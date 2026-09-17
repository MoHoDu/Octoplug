[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][ValidatePattern('^TASK-\d{8}-\d{3}$')][string]$Id,
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9]+(?:-[a-z0-9]+)*$')][string]$Slug,
    [string]$Title = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$template = Join-Path $repoRoot 'tasks\TEMPLATE'
$destination = Join-Path $repoRoot "tasks\active\$Id-$Slug"

if (-not (Test-Path -LiteralPath $template -PathType Container)) {
    throw "Task template not found: $template"
}
if (Test-Path -LiteralPath $destination) {
    throw "Task already exists: $destination"
}

if ($PSCmdlet.ShouldProcess($destination, "Create task from template")) {
    Copy-Item -LiteralPath $template -Destination $destination -Recurse
    $metaPath = Join-Path $destination 'meta.md'
    $content = Get-Content -LiteralPath $metaPath -Raw
    $today = Get-Date -Format 'yyyy-MM-dd'
    $content = $content.Replace('TASK-YYYYMMDD-NNN', $Id)
    $content = $content.Replace('task/TASK-YYYYMMDD-NNN-slug', "task/$Id-$Slug")
    $content = $content.Replace('YYYY-MM-DD', $today)
    if ($Title) {
        $content = $content.Replace('- **Title:**', "- **Title:** $Title")
    }
    Set-Content -LiteralPath $metaPath -Value $content -Encoding utf8
    Write-Output "Created task: $destination"
    Write-Output "Suggested branch: task/$Id-$Slug"
}
