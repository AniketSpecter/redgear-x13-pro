[CmdletBinding()]
param([Parameter(Mandatory=$true)][ValidateSet('dpi','polling','color')][string]$Feature,[Parameter(Mandatory=$true)][string]$Value)
$ErrorActionPreference='Stop'
if(Get-Process Gaming,hid -ErrorAction SilentlyContinue) { throw 'Close the vendor configurator before changing settings.' }
Add-Type -Path (Join-Path $PSScriptRoot 'HidInventory.cs')
Add-Type -Path (Join-Path $PSScriptRoot 'SettingTrial.cs')
$devices=@([X13HidInventory]::Enumerate() | Where-Object { $_.VendorId -eq 0x1b80 -and $_.ProductId -eq 0xb412 -and $_.Path -match '(?i)&mi_01&col03#' -and $_.FeatureBytes -eq 40 -and $_.FeatureReportIds -contains 3 })
if($devices.Count -ne 1) { throw 'Expected exactly one matching settings collection.' }
$result=if($Feature -eq 'color'){[X13SettingTrial]::SetStaticColor($devices[0].Path,$Value)}else{[X13SettingTrial]::Set($devices[0].Path,$Feature,[int]$Value)}
$result | ConvertTo-Json -Depth 4
if(!$result.ReadBackMatched) { throw 'Settings read-back mismatch.' }
