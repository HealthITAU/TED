param(
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "artifacts/publish",
    [ValidateSet("All", "FrameworkDependent", "SelfContained")]
    [string]$DeploymentMode = "All",
    [switch]$EnableUnsafeTrim
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $repoRoot "src/TED/TED.csproj"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishRoot = Join-Path $repoRoot $OutputDirectory
$dotnetHome = Join-Path $artifactsRoot ".dotnet-home"
$nugetPackages = Join-Path $dotnetHome ".nuget/packages"
$appData = Join-Path $artifactsRoot ".appdata"
$localAppData = Join-Path $artifactsRoot ".localappdata"

$targets = @(
    @{ Runtime = "win-x64"; FileSuffix = "x64" },
    @{ Runtime = "win-x86"; FileSuffix = "x86" },
    @{ Runtime = "win-arm64"; FileSuffix = "winarm64" }
)

$deploymentModes = if ($DeploymentMode -eq "All") {
    @("FrameworkDependent", "SelfContained")
}
else {
    @($DeploymentMode)
}

$deploymentOptions = @{
    FrameworkDependent = @{
        FileSuffix = "framework-dependent"
        SelfContained = "false"
        SingleFileCompression = "false"
    }
    SelfContained = @{
        FileSuffix = "self-contained"
        SelfContained = "true"
        SingleFileCompression = "true"
    }
}

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
New-Item -ItemType Directory -Force -Path $dotnetHome | Out-Null
New-Item -ItemType Directory -Force -Path $appData | Out-Null
New-Item -ItemType Directory -Force -Path $localAppData | Out-Null

$env:DOTNET_CLI_HOME = $dotnetHome
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_NOLOGO = "1"
$env:NUGET_PACKAGES = $nugetPackages
$env:APPDATA = $appData
$env:LOCALAPPDATA = $localAppData

$topLevelArtifactNames = @(
    "TED-x64.exe",
    "TED-x86.exe",
    "TED-winarm64.exe"
)

foreach ($mode in $deploymentModes) {
    foreach ($target in $targets) {
        $modeFileSuffix = $deploymentOptions[$mode].FileSuffix
        $topLevelArtifactNames += "TED-$($target.FileSuffix)-$modeFileSuffix.exe"
    }
}

foreach ($artifactName in $topLevelArtifactNames | Select-Object -Unique) {
    $artifactPath = Join-Path $publishRoot $artifactName
    if (Test-Path -LiteralPath $artifactPath) {
        Remove-Item -LiteralPath $artifactPath -Force
    }
}

$publishedArtifacts = @()

foreach ($mode in $deploymentModes) {
    $options = $deploymentOptions[$mode]
    $modeFileSuffix = $options.FileSuffix
    $selfContained = $options.SelfContained
    $singleFileCompression = $options.SingleFileCompression
    $publishTrimmed = if ($EnableUnsafeTrim -and $mode -eq "SelfContained") { "true" } else { "false" }

    foreach ($target in $targets) {
        $runtime = $target.Runtime
        $runtimeOutput = Join-Path $publishRoot (Join-Path $modeFileSuffix $runtime)

        $publishArgs = @(
            "publish", $project,
            "--configuration", $Configuration,
            "--runtime", $runtime,
            "--self-contained", $selfContained,
            "-p:SelfContained=$selfContained",
            "-p:PublishSingleFile=true",
            "-p:PublishTrimmed=$publishTrimmed",
            "-p:EnableCompressionInSingleFile=$singleFileCompression",
            "-p:IncludeNativeLibrariesForSelfExtract=true",
            "-p:PublishReadyToRun=false",
            "-p:DebugType=None",
            "-p:DebugSymbols=false",
            "--output", $runtimeOutput
        )

        dotnet @publishArgs

        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed for $runtime $mode with exit code $LASTEXITCODE"
        }

        $publishedExe = Join-Path $runtimeOutput "TED.exe"
        $finalFileName = "TED-$($target.FileSuffix)-$modeFileSuffix.exe"
        $finalExe = Join-Path $publishRoot $finalFileName
        Copy-Item -Force $publishedExe $finalExe

        $file = Get-Item $finalExe
        $sizeMb = [math]::Round($file.Length / 1MB, 2)
        Write-Host "$($finalFileName): $sizeMb MB"

        $publishedArtifacts += [pscustomobject]@{
            Name = $finalFileName
            Runtime = $runtime
            DeploymentMode = $mode
            SizeMB = $sizeMb
            Path = $finalExe
        }
    }
}

$publishedArtifacts | Sort-Object DeploymentMode, Runtime | Format-Table -AutoSize
