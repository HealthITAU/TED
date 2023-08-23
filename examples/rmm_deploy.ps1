#Your Logo goes here
#This can either be a path to a local file, or a URL
$Pathtologo = 'valuegoeshere'

#Setting some default paths
$Logfile = "C:\ProgramData\TED\Install.log"
$downloadURL = 'https://github.com/HealthITAU/TED/releases/latest/download/TED-x64.exe'
$TEDPath = 'C:\ProgramData\TED\Ted.exe'
$ShortcutLocation = "C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup\TED.lnk"

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

#Create dir and download file if dir not exists
if(!(test-path -Path C:\ProgramData\TED)){
    md C:\ProgramData\TED
    #find Windows Architecture relevant download link
    $platform = try{Get-CimInstance -classname Win32_Processor| Select-Object -ExpandProperty Architecture}
    catch [System.Management.Automation.RuntimeException]{get-wmiobject Win32_Processor | Select-Object -ExpandProperty Architecture}
    Switch($platform){
        0 {
            $downloadURL = 'https://github.com/HealthITAU/TED/releases/latest/download/TED-x86.exe'
            WriteLog '32 bit Processor detected, downloading TED for x86 Architecture'
        }
        9 {
            $downloadURL = 'https://github.com/HealthITAU/TED/releases/latest/download/TED-x64.exe'
            WriteLog '64 bit Processor detected, downloading TED for x64 Architecture'
        }
        12 {
            $downloadURL = 'https://github.com/HealthITAU/TED/releases/latest/download/TED-winarm64.exe'
            WriteLog 'ARM Processor detected, downloading TED for ARM Architecture'
        }
        default{
            $NoPlatform = $true}
        }
        if(!$NoPlatform) {
            wget -OutFile $TEDPath $downloadURL
        }
        else{
            Write-Output "Cannot determine Windows Arcitecture, defaulting to 64bit"
            WriteLog "Cannot determine Windows Arcitecture, defaulting to 64bit"
            wget -OutFile $TEDPath $downloadURL
        }
}

WriteLog "Creating Shortcut with switches to image provided"
if($Pathtologo -eq 'valuegoeshere'){ $Switches = "" }
else{
    $Switches = "-i $($Pathtologo)" 
}
Set-ShortCut $TEDPath $Switches $ShortcutLocation
