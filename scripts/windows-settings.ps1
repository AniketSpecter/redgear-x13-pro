[CmdletBinding()]
param([ValidateSet('status','pointer','precision','scroll','page-scroll','double-click')][string]$Setting='status',[int]$Value)
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'WindowsMouseSettings.cs')
if($Setting -eq 'status'){[X13WindowsMouseSettings]::Read() | ConvertTo-Json}
else{[X13WindowsMouseSettings]::Set($Setting,$Value) | ConvertTo-Json}
