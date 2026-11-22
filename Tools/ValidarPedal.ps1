param([string]$UnityEditor)

$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot
if (!$UnityEditor) {
    $versionLine = Get-Content "$project/ProjectSettings/ProjectVersion.txt" -TotalCount 1
    $version = ($versionLine -split ': ')[1]
    $UnityEditor = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe"
}

$profile = [guid]::NewGuid().ToString('N')
$previousProfile = $env:PEDAL_TEST_PROFILE
$previousReopen = $env:PEDAL_TEST_REOPEN
try {
    $env:PEDAL_TEST_PROFILE = $profile
    foreach ($phase in 'play', 'reopen') {
        $env:PEDAL_TEST_REOPEN = if ($phase -eq 'reopen') { '1' } else { '0' }
        $log = Join-Path ([IO.Path]::GetTempPath()) "pedal-$profile-$phase.log"
        Write-Host "Unity ($phase) - $log"
        & $UnityEditor -batchmode -nographics -projectPath $project -executeMethod VerticalSliceValidator.Run -logFile $log | Out-Host
        if ($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch '[Pedal Test] PASSED' -Quiet)) {
            throw "Unity ($phase) saiu com codigo $LASTEXITCODE. Consulte $log"
        }
    }
} finally {
    $env:PEDAL_TEST_PROFILE = $previousProfile
    $env:PEDAL_TEST_REOPEN = $previousReopen
}
