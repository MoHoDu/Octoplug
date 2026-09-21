[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [switch]$Compile,
    [switch]$RunTests
)

$ErrorActionPreference = 'Stop'
$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$unity = Get-Command unity -ErrorAction Stop

function Convert-UnityResult {
    param([Parameter(Mandatory)][string]$Json)

    $outer = $Json | ConvertFrom-Json
    if (-not $outer.success) {
        $message = @($outer.errors | ForEach-Object message) -join '; '
        throw "Unity command failed: $message"
    }

    $result = $outer.data.result
    if ($result -is [string]) {
        $trimmed = $result.Trim()
        if ($trimmed.StartsWith('{') -or $trimmed.StartsWith('[')) {
            $result = $trimmed | ConvertFrom-Json
        }
    }

    return $result
}

function Wait-Recompile {
    param([ValidateRange(30, 1800)][int]$TimeoutSeconds = 300)

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $raw = (& $unity.Source command recompile_status --project-path $resolvedProject --format json 2>&1) -join "`n"
        if ($LASTEXITCODE -eq 0) {
            try {
                $status = Convert-UnityResult -Json $raw
                if ($status.status -in @('completed', 'up_to_date')) {
                    return $status
                }
            } catch {
                # Domain reload can briefly interrupt or return an incomplete response.
            }
        }

        Start-Sleep -Seconds 2
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Unity recompilation did not complete within $TimeoutSeconds seconds."
}

Write-Output "Unity project: $resolvedProject"
& $unity.Source pipeline list --format json
if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect Unity Pipeline instances.' }

& $unity.Source status --project-path $resolvedProject --format json
if ($LASTEXITCODE -ne 0) {
    Write-Warning 'No Editor was visible to this process. A sandbox can hide a live Editor; no mutation was attempted.'
    exit 2
}

$statusJson = (& $unity.Source command editor_status --project-path $resolvedProject --format json) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Unable to read Editor status.' }
$editor = Convert-UnityResult -Json $statusJson
if ($editor.projectPath -ne $resolvedProject) { throw "Connected Editor path mismatch: $($editor.projectPath)" }
if ($editor.status -ne 'ready' -or $editor.compiling -or $editor.domainReloadInProgress) {
    throw 'Editor is not ready for verification.'
}
if ($editor.playMode -ne 'stopped') { throw "Editor is in Play Mode: $($editor.playMode)" }
Write-Output $statusJson

$scenesJson = (& $unity.Source command list_open_scenes --project-path $resolvedProject --format json) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Unable to list open scenes.' }
$scenes = Convert-UnityResult -Json $scenesJson
$dirtyScenes = @($scenes.scenes | Where-Object isDirty)
if ($dirtyScenes.Count) { throw "Open scene is dirty: $($dirtyScenes.path -join ', ')" }
Write-Output $scenesJson

if ($Compile) {
    # The trigger request can be interrupted by the successful domain reload.
    $null = & $unity.Source command recompile --project-path $resolvedProject --format json 2>&1
    $compileResult = Wait-Recompile
    if ($compileResult.failed -or $compileResult.compilationFailed -or @($compileResult.errors).Count -gt 0) {
        throw "Unity compilation failed: $(@($compileResult.errors) -join '; ')"
    }

    Write-Output "Unity compilation: $($compileResult.status)"
}

if ($RunTests) {
    $testsJson = (& $unity.Source command list_tests --project-path $resolvedProject --format json) -join "`n"
    if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect project tests.' }
    $tests = Convert-UnityResult -Json $testsJson
    $testCount = if ($tests.Count -ne $null) {
        [int]$tests.Count
    } elseif ($tests.tests -ne $null) {
        @($tests.tests).Count
    } else {
        @($tests).Count
    }
    if ($testCount -eq 0) {
        Write-Output 'NO_PROJECT_TESTS: Unity reported zero project tests.'
    } else {
        Write-Output "Tests discovered: $testCount. Run the task-selected suites explicitly."
    }
} else {
    Write-Output 'Tests not requested.'
}
