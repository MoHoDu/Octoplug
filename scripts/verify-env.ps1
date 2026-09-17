[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$failures = [System.Collections.Generic.List[string]]::new()

& git -C $repoRoot check-ignore -q .env
if ($LASTEXITCODE -ne 0) { $failures.Add('.env is not ignored.') }
& git -C $repoRoot check-ignore -q .env.example
if ($LASTEXITCODE -eq 0) { $failures.Add('.env.example is unexpectedly ignored.') }

$envPath = Join-Path $repoRoot '.env'
if (Test-Path -LiteralPath $envPath) {
    $known = @('WORKTREE_ROOT')
    foreach ($line in Get-Content -LiteralPath $envPath) {
        if ($line -match '^\s*(?:#|$)') { continue }
        if ($line -notmatch '^([A-Z][A-Z0-9_]*)=(.*)$') {
            $failures.Add('Malformed .env line detected (value not printed).')
            continue
        }
        if ($known -notcontains $Matches[1]) {
            $failures.Add("Unknown .env key: $($Matches[1])")
        }
    }
}

if ($failures.Count) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}
Write-Output 'OK: environment contract and ignore rules are valid.'
