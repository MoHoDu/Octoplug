[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$failures = [System.Collections.Generic.List[string]]::new()

$required = @(
    'AGENTS.md', 'CLAUDE.md', '.env.example', '.mcp.json',
    'docs/architecture.md', 'docs/domains/INDEX.md',
    'harness/MIGRATION_MAP.md', 'harness/mcp/README.md',
    'tasks/TEMPLATE/meta.md', 'tasks/TEMPLATE/plan.md', 'tasks/TEMPLATE/todo.md',
    'tasks/TEMPLATE/handoff.md', 'tasks/TEMPLATE/log.md',
    'scripts/setup-links.ps1', 'scripts/unity-mcp.ps1'
)
foreach ($relative in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $relative) -PathType Leaf)) {
        $failures.Add("Missing required file: $relative")
    }
}

$domainFiles = Get-ChildItem (Join-Path $repoRoot 'docs\domains') -Filter '*.md' -File -ErrorAction SilentlyContinue |
    Where-Object Name -ne 'INDEX.md'
foreach ($file in $domainFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($match in [regex]::Matches($content, '`((?:Assets|Packages|ProjectSettings)/[^`]+)`')) {
        $path = Join-Path $repoRoot ($match.Groups[1].Value.Replace('/', '\'))
        if (-not (Test-Path -LiteralPath $path)) {
            $failures.Add("Domain map references missing existing path: $($match.Groups[1].Value)")
        }
    }
}

Get-ChildItem (Join-Path $repoRoot '.agents\skills') -Filter 'SKILL.md' -File -Recurse -ErrorAction SilentlyContinue |
    ForEach-Object {
        $text = Get-Content -LiteralPath $_.FullName -Raw
        if ($text -notmatch '(?s)^---\r?\nname: [a-z0-9-]+\r?\ndescription: .+?\r?\n---') {
            $failures.Add("Invalid skill frontmatter: $($_.FullName)")
        }
    }

$protected = & git -C $repoRoot diff --name-only -- Assets Packages ProjectSettings
if ($protected) { $failures.Add("Unexpected Unity-content changes: $($protected -join ', ')") }

try {
    & (Join-Path $PSScriptRoot 'verify-fast.ps1')
} catch {
    $failures.Add("Fast harness verification failed: $($_.Exception.Message)")
}

if ($failures.Count) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}
Write-Output 'OK: harness structure and boundaries passed verification.'
