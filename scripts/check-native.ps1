# Parse scripts and compile P/Invoke helpers without opening any device.
$ErrorActionPreference = 'Stop'
foreach ($file in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1') {
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$null,[ref]$parseErrors) | Out-Null
    if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
}
Add-Type -Path (Join-Path $PSScriptRoot 'HidInventory.cs')
Add-Type -Path (Join-Path $PSScriptRoot 'ReadFeature3.cs')
Add-Type -Path (Join-Path $PSScriptRoot 'SettingTrial.cs')
Add-Type -Path (Join-Path $PSScriptRoot 'WindowsMouseSettings.cs')
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(!(Test-Path -LiteralPath $compiler)){$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'}
$cache=Join-Path (Split-Path $PSScriptRoot -Parent) '.tools/native-compile-check'
New-Item -ItemType Directory -Force -Path $cache | Out-Null
$panelOutput=Join-Path $cache 'X13WindowsSettings.exe'
& $compiler /nologo /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$panelOutput" (Join-Path $PSScriptRoot 'WindowsMouseSettings.cs') (Join-Path $PSScriptRoot 'WindowsMouseSettingsUi.cs')
if($LASTEXITCODE -ne 0){throw 'Native Windows panel compile check failed.'}
Write-Output 'PowerShell scripts parse; native HID helper types compile. No USB request was made.'
