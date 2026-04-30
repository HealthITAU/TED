param (
    [switch]$Uninstall,
    [switch]$Updateself
)

# Customize these values for your environment.
$InstallDir = 'C:\ProgramData\TED'
$GitHubRepo = 'HealthITAU/TED'
$CompanyLogoFileName = 'company-logo.png'
$CompanyLogoDownloadUrl = '' # Optional. Example: 'https://example.com/assets/company-logo.png'
$UpdaterScriptDownloadUrl = '' # Optional. Host your customized copy here if you use -Updateself.
$TaskName = 'Update TED'
$UpdateScheduleDay = 'Tuesday'
$UpdateScheduleTime = '8:00AM'

# Derived paths and release URLs.
$LogoPath = Join-Path $InstallDir $CompanyLogoFileName
$LogFile = Join-Path $InstallDir 'TED.log'
$ReleaseDownloadBaseUrl = "https://github.com/$GitHubRepo/releases/latest/download"
$TEDPath = Join-Path $InstallDir 'TED.exe'
$ShortcutLocation = 'C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup\TED.lnk'
$UpdaterScriptPath = Join-Path $InstallDir 'rmm_deploy.ps1'
$DownloadUrl = "$ReleaseDownloadBaseUrl/TED-x64.exe"

#Function for logging
function WriteLog
{
    Param ([string]$LogString)
    $Stamp = (Get-Date).toString("yyyy/MM/dd HH:mm:ss")
    $LogMessage = "$Stamp $LogString"
    Add-content $LogFile -value $LogMessage
}

#function to create or modify a shortcut
function Set-Shortcut
{
    param ( [string]$SourceExe,[string]$Arguments, [string]$DestinationPath )

    $WshShell = New-Object -comObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($DestinationPath)
    $Shortcut.TargetPath = $SourceExe
    $Shortcut.Arguments = $Arguments
    $Shortcut.Save()
}

function Checkforlogoupdates{
    if(-not [string]::IsNullOrWhiteSpace($CompanyLogoDownloadUrl)){
        if(test-path -Path $LogoPath){
            $wc = [System.Net.WebClient]::new()
            $WebFileHash = Get-FileHash -Algorithm MD5 -InputStream ($wc.OpenRead($CompanyLogoDownloadUrl))
            $LocalFileHash = Get-FileHash -Algorithm MD5 $LogoPath
            if($WebFileHash.Hash -ne $LocalFileHash.Hash){
                WriteLog "File logo at $($CompanyLogoDownloadUrl) is different to local image, replacing local copy"
                Remove-Item $LogoPath
                Invoke-WebRequest -OutFile $LogoPath $CompanyLogoDownloadUrl
            }
        }
        else{
            WriteLog "Downloading requested logo from $($CompanyLogoDownloadUrl)"
            Invoke-WebRequest -OutFile $LogoPath $CompanyLogoDownloadUrl
        }    
    }
}

function setupdateschedule{
$exists = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if($exists -eq $null){
        if([string]::IsNullOrWhiteSpace($UpdaterScriptDownloadUrl)){
            WriteLog "Cannot create update schedule because `$UpdaterScriptDownloadUrl is not set."
            return
        }
        if(!(test-path -Path $UpdaterScriptPath)){
            WriteLog "Downloading the updater script."
            Invoke-WebRequest -OutFile $UpdaterScriptPath $UpdaterScriptDownloadUrl
        }
        $trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek $UpdateScheduleDay -At $UpdateScheduleTime
        $action = New-ScheduledTaskAction -Execute "Powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$($UpdaterScriptPath)`""
        $settings = New-ScheduledTaskSettingsSet  -StartWhenAvailable
        $principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount
        Register-ScheduledTask -TaskName $TaskName -Trigger $trigger -Action $action -Settings $settings -Principal $principal
    }
}

function InstallTED
{
    #Create dir and download file if dir not exists
    if(!(test-path -Path $InstallDir)){
        New-Item -ItemType Directory -Path $InstallDir -Force
        }
        #Download logofile
        if(-not [string]::IsNullOrWhiteSpace($CompanyLogoDownloadUrl)){
            if(!(test-path -Path $LogoPath)){
                WriteLog "Downloading requested logo from $($CompanyLogoDownloadUrl)"
                Invoke-WebRequest -OutFile $LogoPath $CompanyLogoDownloadUrl
                }
                else{
                Checkforlogoupdates
                }
            }
        #find Windows Architecture relevant download link
        $platform = try{Get-CimInstance -classname Win32_Processor| Select-Object -ExpandProperty Architecture}
        catch [System.Management.Automation.RuntimeException]{get-wmiobject Win32_Processor | Select-Object -ExpandProperty Architecture}
        Switch($platform) {
            0 {
                $DownloadUrl = "$ReleaseDownloadBaseUrl/TED-x86.exe"
                WriteLog '32 bit Processor detected, downloading TED for x86 Architecture'
            }
            9 {
                $DownloadUrl = "$ReleaseDownloadBaseUrl/TED-x64.exe"
                WriteLog '64 bit Processor detected, downloading TED for x64 Architecture'
            }
            12 {
                $DownloadUrl = "$ReleaseDownloadBaseUrl/TED-winarm64.exe"
                WriteLog 'ARM Processor detected, downloading TED for ARM Architecture'
            }
            default{ $NoPlatform = $true }
        }
            
        if(!$NoPlatform) {
            Invoke-WebRequest -OutFile $TEDPath $DownloadUrl
        }
        else {
            Write-Output "Cannot determine Windows Architecture, defaulting to 64bit"
            WriteLog "Cannot determine Windows Architecture, defaulting to 64bit"
            Invoke-WebRequest -OutFile $TEDPath $DownloadUrl
        }
    WriteLog "Creating Shortcut with switches to image provided"
    if([string]::IsNullOrWhiteSpace($CompanyLogoDownloadUrl) -and !(Test-Path $LogoPath)){ $Switches = "" }
    else{
        $Switches = "-i `"$($LogoPath)`""
    }
    Set-ShortCut $TEDPath $Switches $ShortcutLocation
    if($Updateself){
        WriteLog "Setting autoupdates using Windows Scheduler."
        setupdateschedule
    }
}

function UninstallTED
{
    Remove-Item -Path $InstallDir -Recurse -Force
    Remove-Item $ShortcutLocation -ErrorAction SilentlyContinue
    $exists = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
    if($exists -ne $null){
        Unregister-ScheduledTask -TaskName "$TaskName" -Confirm:$false
    }
}

function Rotatelogs
{
if(Test-Path $Logfile){
    if((Get-Item $Logfile).Length -gt 5MB){
        if(Test-Path $Logfile".old"){
            del $Logfile".old"
            }
            Rename-Item -Path $Logfile -NewName $Logfile".old"
            WriteLog "Rotated Log File."
        }
    }
}

function Main
{
    if($Uninstall){
    UninstallTED
    }
    else{
        if(test-path -Path $TEDPath){
            $newestversion = ((Invoke-WebRequest -Uri "$ReleaseDownloadBaseUrl/" -MaximumRedirection 0 -UseBasicParsing -ErrorAction:SilentlyContinue).Headers.Location|Split-Path -Leaf) -replace "[a-zA-Z]"
            $installedversion = (Get-Item $TEDPath).VersionInfo.FileVersion
            if($newestversion -ne $installedversion){
                WriteLog "Newer version of TED located, removing old version"
                Remove-Item $TEDPath
                InstallTED
            }
            else{
                WriteLog "TED is up to date"
                Checkforlogoupdates
            }
        }
        else{
            InstallTED
        }
    Rotatelogs    
    }
}

Main
