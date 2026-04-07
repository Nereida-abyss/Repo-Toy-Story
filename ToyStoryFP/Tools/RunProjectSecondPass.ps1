param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.2.10f1\Editor\Unity.exe",
    [string]$ProjectPath = (Resolve-Path ".").Path
)

if (-not (Test-Path $UnityPath)) {
    Write-Error "No se encontro Unity en: $UnityPath"
    exit 1
}

$logDir = Join-Path $ProjectPath "Temp"
if (-not (Test-Path $logDir)) {
    New-Item -Path $logDir -ItemType Directory | Out-Null
}

$steps = @(
    @{ Name = "Preview"; Method = "ProjectMaintenanceBatchRunner.RunStagedSafePreview"; Log = "Codex_SecondPass_Preview.log" },
    @{ Name = "Apply"; Method = "ProjectMaintenanceBatchRunner.RunStagedSafeApply"; Log = "Codex_SecondPass_Apply.log" },
    @{ Name = "Validation"; Method = "ProjectMaintenanceBatchRunner.RunStagedSafeValidation"; Log = "Codex_SecondPass_Validation.log" }
)

foreach ($step in $steps) {
    $logPath = Join-Path $logDir $step.Log
    Write-Host "Ejecutando $($step.Name) ..."

    & $UnityPath `
        -batchmode `
        -nographics `
        -quit `
        -projectPath $ProjectPath `
        -executeMethod $step.Method `
        -logFile $logPath

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Fallo en paso $($step.Name). Revisa log: $logPath"
        exit $LASTEXITCODE
    }
}

Write-Host "Segunda pasada completada. Logs en: $logDir"
