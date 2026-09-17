[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [switch]$Compile,
    [switch]$RunTests
)

$ErrorActionPreference = 'Stop'
$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$unity = Get-Command unity -ErrorAction Stop

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
$statusResult = $statusJson | ConvertFrom-Json
$editor = $statusResult.data.result
if ($editor.projectPath -ne $resolvedProject) { throw "Connected Editor path mismatch: $($editor.projectPath)" }
if ($editor.status -ne 'ready' -or $editor.compiling -or $editor.domainReloadInProgress) {
    throw 'Editor is not ready for verification.'
}
if ($editor.playMode -ne 'stopped') { throw "Editor is in Play Mode: $($editor.playMode)" }
Write-Output $statusJson

$scenesJson = (& $unity.Source command list_open_scenes --project-path $resolvedProject --format json) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Unable to list open scenes.' }
$scenesResult = $scenesJson | ConvertFrom-Json
$dirtyScenes = @($scenesResult.data.result.scenes | Where-Object isDirty)
if ($dirtyScenes.Count) { throw "Open scene is dirty: $($dirtyScenes.path -join ', ')" }
Write-Output $scenesJson

if ($Compile) {
    & $unity.Source command refresh_and_wait_for_compile --project-path $resolvedProject --format json
    if ($LASTEXITCODE -ne 0) { throw 'Unity compilation failed or was unavailable.' }
}

if ($RunTests) {
    $testsJson = (& $unity.Source command list_tests --project-path $resolvedProject --format json) -join "`n"
    if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect project tests.' }
    $testsResult = $testsJson | ConvertFrom-Json
    $testCount = [int]$testsResult.data.result.Count
    if ($testCount -eq 0) {
        Write-Output 'NO_PROJECT_TESTS: Unity reported zero project tests.'
    } else {
        Write-Output "Tests discovered: $testCount. Run the task-selected suites explicitly."
    }
} else {
    Write-Output 'Tests not requested.'
}
