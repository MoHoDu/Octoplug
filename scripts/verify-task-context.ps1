[CmdletBinding()]
param(
    # 특정 Task만 검사하고 싶을 때 사용.
    # 생략하면 tasks/active 아래의 모든 TASK-* 폴더를 검사한다.
    [string]$TaskPath,

    # 기본 실행에서는 경고만 출력하고 성공 코드(0)로 종료한다.
    # -Strict 사용 시 HARD threshold 초과가 있으면 실패 코드로 종료한다.
    [switch]$Strict
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

# task-context-compaction.md의 기준과 맞춘다.
$limits = [ordered]@{
    'meta.md' = @{
        Warn = 40
        Hard = 50
    }
    'plan.md' = @{
        Warn = 100
        Hard = 120
    }
    'todo.md' = @{
        Warn = 90
        Hard = 110
    }
    'handoff.md' = @{
        Warn = 50
        Hard = 60
    }
    'log.md' = @{
        Warn = 130
        Hard = 160
    }
}

function Get-TaskDirectories {
    if ($TaskPath) {
        $resolved = Resolve-Path $TaskPath -ErrorAction Stop

        if (-not (Test-Path $resolved.Path -PathType Container)) {
            throw "TaskPath is not a directory: $TaskPath"
        }

        return @($resolved.Path)
    }

    $activeRoot = Join-Path $repoRoot 'tasks/active'

    if (-not (Test-Path $activeRoot -PathType Container)) {
        return @()
    }

    return @(
        Get-ChildItem `
            -Path $activeRoot `
            -Directory `
            -Filter 'TASK-*' `
            | Sort-Object Name `
            | ForEach-Object { $_.FullName }
    )
}

function Get-LineCount {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path $Path -PathType Leaf)) {
        return $null
    }

    return @(Get-Content -Path $Path).Count
}

$taskDirectories = @(Get-TaskDirectories)

if ($taskDirectories.Count -eq 0) {
    Write-Output 'Task Context: OK (no active tasks found)'
    exit 0
}

$overallRank = 0
$hardExceeded = $false

Write-Output ''
Write-Output '=== Task Context Budget ==='

foreach ($taskDirectory in $taskDirectories) {
    $taskName = Split-Path $taskDirectory -Leaf

    Write-Output ''
    Write-Output "Task: $taskName"

    $taskRank = 0
    $totalLines = 0

    foreach ($fileName in $limits.Keys) {
        $filePath = Join-Path $taskDirectory $fileName
        $lineCount = Get-LineCount -Path $filePath

        if ($null -eq $lineCount) {
            Write-Output ("  {0,-12} {1}" -f $fileName, 'MISSING')
            $taskRank = [Math]::Max($taskRank, 1)
            continue
        }

        $warnLimit = $limits[$fileName].Warn
        $hardLimit = $limits[$fileName].Hard

        $totalLines += $lineCount

        if ($lineCount -gt $hardLimit) {
            $status = 'COMPACT_REQUIRED'
            $taskRank = [Math]::Max($taskRank, 2)
            $hardExceeded = $true
        }
        elseif ($lineCount -gt $warnLimit) {
            $status = 'WARN'
            $taskRank = [Math]::Max($taskRank, 1)
        }
        else {
            $status = 'OK'
        }

        Write-Output (
            "  {0,-12} {1,4} lines  (warn {2}, hard {3})  {4}" -f `
                $fileName,
                $lineCount,
                $warnLimit,
                $hardLimit,
                $status
        )
    }

    switch ($taskRank) {
        0 { $taskStatus = 'OK' }
        1 { $taskStatus = 'WARN' }
        default { $taskStatus = 'COMPACT_REQUIRED' }
    }

    Write-Output "  Total default-read lines: $totalLines"
    Write-Output "  Result: $taskStatus"

    $overallRank = [Math]::Max($overallRank, $taskRank)
}

switch ($overallRank) {
    0 { $overallStatus = 'OK' }
    1 { $overallStatus = 'WARN' }
    default { $overallStatus = 'COMPACT_REQUIRED' }
}

Write-Output ''
Write-Output "Task Context: $overallStatus"

if ($Strict -and $hardExceeded) {
    Write-Error 'Task context hard threshold exceeded. Compact the active task before substantial new work.'
    exit 2
}

# 기본 모드에서는 context budget 초과가 verify-fast 자체를 막지 않는다.
exit 0