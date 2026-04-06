param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Project = "KillPort.App\\KillPort.App.csproj",
    [string]$InstallerScript = "KillPort.iss",
    [string]$PublishDir = "publish",
    [string]$IsccPath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSCommandPath
$projectPath = Join-Path $root $Project
$installerPath = Join-Path $root $InstallerScript
$publishPath = Join-Path $root $PublishDir

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project file not found: $projectPath"
}

if (-not (Test-Path -LiteralPath $installerPath)) {
    throw "Inno Setup script not found: $installerPath"
}

Write-Host "Publishing application..."
dotnet publish $projectPath -c $Configuration -r $Runtime --self-contained false -o $publishPath
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

if (-not $IsccPath) {
    $isccCommand = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($isccCommand) {
        $IsccPath = $isccCommand.Source
    }
}

if (-not $IsccPath) {
    $registryCommands = @(
        "HKCR:\InnoSetupScriptFile\shell\open\command",
        "HKLM:\SOFTWARE\Classes\InnoSetupScriptFile\shell\open\command"
    )

    foreach ($registryCommand in $registryCommands) {
        if (-not (Test-Path -LiteralPath $registryCommand)) {
            continue
        }

        $commandValue = (Get-ItemProperty -LiteralPath $registryCommand)."(default)"
        if ([string]::IsNullOrWhiteSpace($commandValue)) {
            continue
        }

        $matchedPath = [regex]::Match($commandValue, '"(?<path>[^"]+\\Compil32\.exe)"')
        if (-not $matchedPath.Success) {
            continue
        }

        $candidateIsccPath = Join-Path (Split-Path -Parent $matchedPath.Groups["path"].Value) "ISCC.exe"
        if (Test-Path -LiteralPath $candidateIsccPath) {
            $IsccPath = $candidateIsccPath
            break
        }
    }
}

if (-not $IsccPath) {
    $candidates = @(
        "${env:ProgramFiles(x86)}\\Inno Setup 6\\ISCC.exe",
        "${env:ProgramFiles}\\Inno Setup 6\\ISCC.exe"
    )

    $IsccPath = $candidates |
        Where-Object { $_ -and (Test-Path -LiteralPath $_) } |
        Select-Object -First 1
}

if (-not $IsccPath) {
    throw "ISCC.exe was not found. Install Inno Setup 6 or pass -IsccPath."
}

Write-Host "Building installer with Inno Setup..."
& $IsccPath $installerPath
if ($LASTEXITCODE -ne 0) {
    throw "ISCC.exe failed with exit code $LASTEXITCODE."
}

$setupPath = Join-Path $root "KillPort-Setup.exe"
if (Test-Path -LiteralPath $setupPath) {
    Write-Host "Installer created at: $setupPath"
}
else {
    Write-Host "Build completed."
}
