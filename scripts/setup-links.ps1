[CmdletBinding(SupportsShouldProcess)]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$source = Join-Path $repoRoot '.agents\skills'
$target = Join-Path $repoRoot '.claude\skills'
$targetParent = Split-Path $target -Parent

if (-not (Test-Path -LiteralPath $source -PathType Container)) {
    throw "Shared skills directory does not exist: $source"
}

if (Test-Path -LiteralPath $target) {
    $item = Get-Item -LiteralPath $target -Force
    if (-not $item.LinkType) {
        throw "Refusing to replace real directory: $target"
    }

    $resolvedSource = (Resolve-Path -LiteralPath $source).Path
    $linkTarget = @($item.Target)[0]
    if (-not [System.IO.Path]::IsPathRooted($linkTarget)) {
        $linkTarget = Join-Path $item.Parent.FullName $linkTarget
    }
    $resolvedLinkTarget = (Resolve-Path -LiteralPath $linkTarget).Path
    if ($resolvedLinkTarget -ne $resolvedSource) {
        throw "Existing link points somewhere else: $target -> $resolvedLinkTarget"
    }

    Write-Output "OK: skills adapter already points to $resolvedSource"
    exit 0
}

if ($PSCmdlet.ShouldProcess($target, "Create junction to $source")) {
    if (-not (Test-Path -LiteralPath $targetParent)) {
        New-Item -ItemType Directory -Path $targetParent | Out-Null
    }

    New-Item -ItemType Junction -Path $target -Target $source | Out-Null
    $created = Get-Item -LiteralPath $target -Force
    $resolvedSource = (Resolve-Path -LiteralPath $source).Path
    $linkTarget = @($created.Target)[0]
    if (-not [System.IO.Path]::IsPathRooted($linkTarget)) {
        $linkTarget = Join-Path $created.Parent.FullName $linkTarget
    }
    $resolvedLinkTarget = (Resolve-Path -LiteralPath $linkTarget).Path
    if ($resolvedLinkTarget -ne $resolvedSource) {
        throw "Junction validation failed: $target"
    }
    Write-Output "Created local skills adapter: $target -> $source"
}
