[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [string]$TestFilter = 'RuntimeUpgradeTests',
    [ValidateRange(30, 1800)][int]$TimeoutSeconds = 300
)

$ErrorActionPreference = 'Stop'
$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$unity = Get-Command unity -ErrorAction Stop

$results = [ordered]@{
    Build = 'SKIPPED'
    UnityCompile = 'SKIPPED'
    UnityTests = 'SKIPPED'
    RuntimeVerification = 'SKIPPED'
    ConsoleDelta = 'UNRELIABLE'
    NewConsoleErrors = 'UNRELIABLE'
    NewConsoleWarnings = 'UNRELIABLE'
    VerifyFast = 'SKIPPED'
    Overall = 'FAIL'
}
$failed = $false
$consoleBaseline = $null

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

function Invoke-UnityCommand {
    param(
        [Parameter(Mandatory)][string]$Name,
        [string[]]$Arguments = @(),
        [switch]$AllowFailure
    )

    $output = & $unity.Source command $Name @Arguments --project-path $resolvedProject --format json 2>&1
    $exitCode = $LASTEXITCODE
    $text = ($output | ForEach-Object { $_.ToString() }) -join "`n"
    if ($exitCode -ne 0) {
        if ($AllowFailure) {
            return $null
        }

        throw "Unity command '$Name' failed (exit $exitCode): $text"
    }

    return Convert-UnityResult -Json $text
}

function Wait-UnityCommand {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string[]]$TerminalStates,
        [Parameter(Mandatory)][int]$Timeout
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($Timeout)
    do {
        $result = Invoke-UnityCommand -Name $Name -AllowFailure
        if ($null -ne $result -and $TerminalStates -contains [string]$result.status) {
            return $result
        }

        Start-Sleep -Seconds 2
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Unity command '$Name' did not reach $($TerminalStates -join '/') within $Timeout seconds."
}

function Assert-EditorReady {
    $editor = Invoke-UnityCommand -Name 'editor_status'
    if ([IO.Path]::GetFullPath([string]$editor.projectPath) -ne $resolvedProject) {
        throw "Connected Editor path mismatch: $($editor.projectPath)"
    }
    if ($editor.status -ne 'ready' -or $editor.compiling -or $editor.domainReloadInProgress) {
        throw "Editor is not ready: $($editor.status)"
    }
    if ($editor.playMode -ne 'stopped') {
        throw "Editor is in Play Mode: $($editor.playMode)"
    }

    $scenes = Invoke-UnityCommand -Name 'list_open_scenes'
    $dirtyScenes = @($scenes.scenes | Where-Object isDirty)
    if ($dirtyScenes.Count -gt 0) {
        throw "Open scene is dirty: $($dirtyScenes.path -join ', ')"
    }
}

try {
    $consoleBaseline = Invoke-UnityCommand -Name 'console_status'
    Write-Output ("Console baseline: cursor={0}, session={1}, sampled={2}, errors={3}, warnings={4}" -f
        $consoleBaseline.cursor,
        $consoleBaseline.session,
        $consoleBaseline.groundTruth.sampledUtc,
        $consoleBaseline.groundTruth.consoleErrors,
        $consoleBaseline.groundTruth.consoleWarnings)
} catch {
    Write-Warning "Console baseline failed: $($_.Exception.Message)"
    $failed = $true
}

try {
    if ([IO.Path]::GetFullPath($repoRoot) -ne $resolvedProject) {
        throw "verify-task.ps1 must run against its containing worktree: $repoRoot"
    }

    $solutions = @(Get-ChildItem -LiteralPath $resolvedProject -Filter '*.slnx' -File)
    if ($solutions.Count -ne 1) {
        throw "Expected exactly one .slnx in '$resolvedProject'; found $($solutions.Count)."
    }

    & dotnet build $solutions[0].FullName --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
    $results.Build = 'PASS'
} catch {
    $results.Build = "FAIL - $($_.Exception.Message)"
    $failed = $true
}

try {
    Assert-EditorReady
    $null = Invoke-UnityCommand -Name 'recompile' -AllowFailure
    $compile = Wait-UnityCommand -Name 'recompile_status' -TerminalStates @('completed', 'up_to_date') -Timeout $TimeoutSeconds
    if ($compile.failed -or $compile.compilationFailed -or @($compile.errors).Count -gt 0) {
        throw "Unity compilation reported errors: $(@($compile.errors) -join '; ')"
    }
    Assert-EditorReady
    $results.UnityCompile = "PASS - $($compile.status)"
} catch {
    $results.UnityCompile = "FAIL - $($_.Exception.Message)"
    $failed = $true
}

try {
    if (-not $results.UnityCompile.StartsWith('PASS')) {
        throw 'Unity compile/readiness failed; tests were not started.'
    }

    $testList = Invoke-UnityCommand -Name 'list_tests' -Arguments @('--mode', 'editor')
    $tests = @($testList.tests)
    if ($tests.Count -eq 0 -and $testList -is [System.Array]) {
        $tests = @($testList)
    }
    if ($TestFilter) {
        $tests = @($tests | Where-Object {
            ([string]$_.name).Contains($TestFilter) -or
            ([string]$_.fullName).Contains($TestFilter)
        })
    }
    if ($tests.Count -eq 0) {
        throw "Unity discovered zero EditMode tests for filter '$TestFilter'."
    }

    $runArguments = @('--mode', 'editor', '--async_tests', 'true', '--timeout', $TimeoutSeconds)
    if ($TestFilter) {
        $runArguments += @('--filter', $TestFilter, '--filter_type', 'testName')
    }
    $start = Invoke-UnityCommand -Name 'run_tests' -Arguments $runArguments
    if ($start.success -eq $false -or ($start.result -and $start.result -ne 'running')) {
        throw "Unity tests did not start: $($start.message)"
    }

    $testStatus = Wait-UnityCommand -Name 'test_status' -TerminalStates @('completed', 'failed', 'cancelled', 'timeout') -Timeout ($TimeoutSeconds + 30)
    if ($testStatus.status -ne 'completed') {
        throw "Unity test run ended with status '$($testStatus.status)'."
    }

    $summary = $testStatus.summary
    if ($null -eq $summary) {
        throw 'Unity test status completed without a summary.'
    }

    $completedTests = @($testStatus.results)
    $powerStripTests = @($completedTests | Where-Object {
        ([string]$_.FullName).Contains('PowerStripRuntimeUpgradeTests')
    })
    $wallOutletTests = @($completedTests | Where-Object {
        ([string]$_.FullName).Contains('WallOutletRuntimeUpgradeTests')
    })
    $powerStripPassed = @($powerStripTests | Where-Object Status -eq 'Passed').Count
    $wallOutletPassed = @($wallOutletTests | Where-Object Status -eq 'Passed').Count
    $suiteCounts = "PowerStrip $powerStripPassed/$($powerStripTests.Count), WallOutlet $wallOutletPassed/$($wallOutletTests.Count)"
    $results.UnityTests = "PASS - discovered $($tests.Count), passed $($summary.passed)/$($summary.total); $suiteCounts"
    $results.RuntimeVerification = "PASS - official EditMode filter '$TestFilter'; $suiteCounts"
    if ([int]$summary.total -eq 0 -or [int]$summary.failed -gt 0 -or [int]$summary.passed -ne [int]$summary.total) {
        throw "Unity tests failed: passed=$($summary.passed), failed=$($summary.failed), total=$($summary.total)."
    }
} catch {
    $results.UnityTests = "FAIL - $($_.Exception.Message)"
    $results.RuntimeVerification = "FAIL - official EditMode filter '$TestFilter'"
    $failed = $true
}

if ($null -ne $consoleBaseline) {
    try {
        $delta = Invoke-UnityCommand -Name 'console' -Arguments @(
            '--since', [string]$consoleBaseline.cursor,
            '--since_session', [string]$consoleBaseline.session,
            '--tail', '1000')
        if ($delta.reset -or $delta.dropped -or $delta.session -ne $consoleBaseline.session) {
            throw 'Console cursor/session continuity was lost.'
        }

        $results.ConsoleDelta = 'RELIABLE'
        $entries = @($delta.entries)
        $errorCount = @($entries | Where-Object level -eq 'error').Count
        $warningCount = @($entries | Where-Object level -eq 'warn').Count
        $results.NewConsoleErrors = if ($errorCount -eq 0) { 'PASS - 0' } else { "FAIL - $errorCount" }
        $results.NewConsoleWarnings = "PASS - $warningCount"
        if ($errorCount -gt 0) { $failed = $true }
    } catch {
        $results.ConsoleDelta = "UNRELIABLE - $($_.Exception.Message)"
        $results.NewConsoleErrors = "UNRELIABLE - $($_.Exception.Message)"
        $results.NewConsoleWarnings = "UNRELIABLE - $($_.Exception.Message)"
        $failed = $true
    }
}

try {
    & (Join-Path $PSScriptRoot 'verify-fast.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'verify-fast.ps1 failed.' }
    $results.VerifyFast = 'PASS'
} catch {
    $results.VerifyFast = "FAIL - $($_.Exception.Message)"
    $failed = $true
}

$results.Overall = if ($failed) { 'FAIL' } else { 'PASS' }
Write-Output ''
Write-Output "Build: $($results.Build)"
Write-Output "Unity Compile: $($results.UnityCompile)"
Write-Output "Unity Tests: $($results.UnityTests)"
Write-Output "Runtime Verification: $($results.RuntimeVerification)"
Write-Output "Console Delta: $($results.ConsoleDelta)"
Write-Output "New Console Errors: $($results.NewConsoleErrors)"
Write-Output "New Console Warnings: $($results.NewConsoleWarnings)"
Write-Output "verify-fast: $($results.VerifyFast)"
Write-Output "Overall: $($results.Overall)"

if ($failed) { exit 1 }
