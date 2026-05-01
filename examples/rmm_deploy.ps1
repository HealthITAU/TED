param (
    [switch]$Uninstall,
    [switch]$UpdateSelf,

    [ValidateSet('self-contained', 'framework-dependent')]
    [string]$DeploymentType = 'self-contained'
)

# Customize these values for your environment.
$InstallDir = 'C:\ProgramData\TED'
$GitHubRepo = 'HealthITAU/TED'
$CompanyLogoFileName = 'company-logo.png'
$CompanyLogoDownloadUrl = '' # Optional. Example: 'https://example.com/assets/company-logo.png'
$UpdaterScriptDownloadUrl = '' # Optional. Host your customized copy here if you use -UpdateSelf.
$TaskName = 'Update TED'
$UpdateScheduleDay = 'Tuesday'
$UpdateScheduleTime = '8:00AM'

# Derived paths and release URLs.
$LogoPath = Join-Path -Path $InstallDir -ChildPath $CompanyLogoFileName
$LogFile = Join-Path -Path $InstallDir -ChildPath 'TED.log'
$ReleaseDownloadBaseUrl = "https://github.com/$GitHubRepo/releases/latest/download"
$ReleaseLatestUrl = "https://github.com/$GitHubRepo/releases/latest"
$TedPath = Join-Path -Path $InstallDir -ChildPath 'TED.exe'
$ShortcutLocation = 'C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup\TED.lnk'
$UpdaterScriptPath = Join-Path -Path $InstallDir -ChildPath 'rmm_deploy.ps1'

function Write-Log {
    param (
        [Parameter(Mandatory)]
        [string]$Message
    )

    $timestamp = Get-Date -Format 'yyyy/MM/dd HH:mm:ss'
    Add-Content -Path $LogFile -Value "$timestamp $Message"
}

function Set-Shortcut {
    param (
        [Parameter(Mandatory)]
        [string]$SourceExe,

        [string]$Arguments = '',

        [Parameter(Mandatory)]
        [string]$DestinationPath
    )

    $wshShell = New-Object -ComObject WScript.Shell
    $shortcut = $wshShell.CreateShortcut($DestinationPath)
    $shortcut.TargetPath = $SourceExe
    $shortcut.Arguments = $Arguments
    $shortcut.Save()
}

function Get-TedDownloadUrl {
    param (
        [Parameter(Mandatory)]
        [string]$Architecture
    )

    if ($DeploymentType -eq 'framework-dependent') {
        return "$ReleaseDownloadBaseUrl/TED-$Architecture-framework-dependent.exe"
    }

    return "$ReleaseDownloadBaseUrl/TED-$Architecture.exe"
}

function Get-WindowsArchitecture {
    try {
        return Get-CimInstance -ClassName Win32_Processor | Select-Object -First 1 -ExpandProperty Architecture
    }
    catch {
        return Get-WmiObject -Class Win32_Processor | Select-Object -First 1 -ExpandProperty Architecture
    }
}

function Get-LatestTedVersion {
    $location = $null

    try {
        $response = Invoke-WebRequest -Uri $ReleaseLatestUrl -MaximumRedirection 0 -UseBasicParsing -ErrorAction Stop
        $location = $response.Headers.Location
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.Headers) {
            $location = $_.Exception.Response.Headers['Location']
        }
    }

    if ([string]::IsNullOrWhiteSpace($location)) {
        Write-Log "Unable to determine the latest TED release version from $ReleaseLatestUrl."
        return $null
    }

    return (Split-Path -Path $location -Leaf) -replace '[a-zA-Z]'
}

function Update-CompanyLogo {
    if ([string]::IsNullOrWhiteSpace($CompanyLogoDownloadUrl)) {
        return
    }

    if (-not (Test-Path -Path $LogoPath)) {
        Write-Log "Downloading company logo from $CompanyLogoDownloadUrl."
        Invoke-WebRequest -Uri $CompanyLogoDownloadUrl -OutFile $LogoPath
        return
    }

    $webClient = [System.Net.WebClient]::new()

    try {
        $remoteLogoHash = Get-FileHash -Algorithm MD5 -InputStream ($webClient.OpenRead($CompanyLogoDownloadUrl))
        $localLogoHash = Get-FileHash -Algorithm MD5 -Path $LogoPath
    }
    finally {
        $webClient.Dispose()
    }

    if ($remoteLogoHash.Hash -eq $localLogoHash.Hash) {
        return
    }

    Write-Log "Company logo at $CompanyLogoDownloadUrl has changed; replacing the local copy."
    Remove-Item -Path $LogoPath -Force
    Invoke-WebRequest -Uri $CompanyLogoDownloadUrl -OutFile $LogoPath
}

function Register-TedUpdateTask {
    $existingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue

    if ($null -ne $existingTask) {
        return
    }

    if ([string]::IsNullOrWhiteSpace($UpdaterScriptDownloadUrl)) {
        Write-Log "Cannot create update schedule because `$UpdaterScriptDownloadUrl is not set."
        return
    }

    if (-not (Test-Path -Path $UpdaterScriptPath)) {
        Write-Log "Downloading updater script from $UpdaterScriptDownloadUrl."
        Invoke-WebRequest -Uri $UpdaterScriptDownloadUrl -OutFile $UpdaterScriptPath
    }

    $trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek $UpdateScheduleDay -At $UpdateScheduleTime
    $action = New-ScheduledTaskAction -Execute 'PowerShell.exe' -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$UpdaterScriptPath`""
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable
    $principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount

    Register-ScheduledTask -TaskName $TaskName -Trigger $trigger -Action $action -Settings $settings -Principal $principal
}

function Install-Ted {
    if (-not (Test-Path -Path $InstallDir)) {
        New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    }

    Update-CompanyLogo

    $downloadUrl = Get-TedDownloadUrl -Architecture 'x64'
    $architecture = Get-WindowsArchitecture

    switch ($architecture) {
        0 {
            $downloadUrl = Get-TedDownloadUrl -Architecture 'x86'
            Write-Log "32-bit processor detected; downloading TED $DeploymentType for x86."
        }
        9 {
            $downloadUrl = Get-TedDownloadUrl -Architecture 'x64'
            Write-Log "64-bit processor detected; downloading TED $DeploymentType for x64."
        }
        12 {
            $downloadUrl = Get-TedDownloadUrl -Architecture 'winarm64'
            Write-Log "ARM64 processor detected; downloading TED $DeploymentType for ARM64."
        }
        default {
            Write-Output "Cannot determine Windows architecture; defaulting to x64."
            Write-Log "Cannot determine Windows architecture; defaulting to x64."
        }
    }

    Invoke-WebRequest -Uri $downloadUrl -OutFile $TedPath

    $shortcutArguments = ''

    if ((-not [string]::IsNullOrWhiteSpace($CompanyLogoDownloadUrl)) -or (Test-Path -Path $LogoPath)) {
        $shortcutArguments = "-i `"$LogoPath`""
    }

    Write-Log "Creating startup shortcut for TED."
    Set-Shortcut -SourceExe $TedPath -Arguments $shortcutArguments -DestinationPath $ShortcutLocation

    if ($UpdateSelf) {
        Write-Log "Configuring automatic TED updates with Windows Task Scheduler."
        Register-TedUpdateTask
    }
}

function Uninstall-Ted {
    if (Test-Path -Path $InstallDir) {
        Remove-Item -Path $InstallDir -Recurse -Force
    }

    Remove-Item -Path $ShortcutLocation -ErrorAction SilentlyContinue

    $existingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue

    if ($null -ne $existingTask) {
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    }
}

function Rotate-Logs {
    if (-not (Test-Path -Path $LogFile)) {
        return
    }

    if ((Get-Item -Path $LogFile).Length -le 5MB) {
        return
    }

    $archiveLogFile = "$LogFile.old"

    if (Test-Path -Path $archiveLogFile) {
        Remove-Item -Path $archiveLogFile -Force
    }

    Move-Item -Path $LogFile -Destination $archiveLogFile -Force
    Write-Log 'Rotated log file.'
}

function Invoke-Main {
    if ($Uninstall) {
        Uninstall-Ted
        return
    }

    if (-not (Test-Path -Path $TedPath)) {
        Install-Ted
        Rotate-Logs
        return
    }

    $latestVersion = Get-LatestTedVersion
    $installedVersion = (Get-Item -Path $TedPath).VersionInfo.FileVersion

    if ($null -eq $latestVersion) {
        Update-CompanyLogo
    }
    elseif ($latestVersion -ne $installedVersion) {
        Write-Log "TED $latestVersion is available; replacing installed version $installedVersion."
        Remove-Item -Path $TedPath -Force
        Install-Ted
    }
    else {
        Write-Log 'TED is up to date.'
        Update-CompanyLogo
    }

    Rotate-Logs
}

Invoke-Main
