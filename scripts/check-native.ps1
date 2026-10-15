# Parse scripts and compile P/Invoke helpers without opening any device.
$ErrorActionPreference = 'Stop'
foreach ($file in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1') {
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$null,[ref]$parseErrors) | Out-Null
    if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
}
Add-Type -Path (Join-Path $PSScriptRoot 'HidInventory.cs')
Add-Type -Path (Join-Path $PSScriptRoot 'ReadFeature3.cs')
Write-Output 'PowerShell scripts parse; native HID helper types compile. No USB request was made.'
