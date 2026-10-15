[CmdletBinding()]
param([string]$Label = 'baseline')
$ErrorActionPreference = 'Stop'
if ($Label -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Invalid label.' }
$root = Split-Path $PSScriptRoot -Parent
$raw = Join-Path $root 'evidence/raw'
$safe = Join-Path $root 'evidence/sanitized'
New-Item -ItemType Directory -Force -Path $raw,$safe | Out-Null
if (-not ('X13HidInventory' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'HidInventory.cs') }
$rows = @([X13HidInventory]::Enumerate())
$rows | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $raw "hid-$Label.json") -Encoding UTF8
$sanitized = @($rows | ForEach-Object {
    [ordered]@{ vid = ('{0:X4}' -f $_.VendorId); pid = ('{0:X4}' -f $_.ProductId); deviceRevision = ('0x{0:X4}' -f $_.DeviceRevision); product = $_.Product; manufacturer = $_.Manufacturer; interface = if ($_.Path -match '(?i)&mi_([0-9a-f]{2})') { $Matches[1] } else { $null }; collection = if ($_.Path -match '(?i)&col([0-9a-f]{2})') { $Matches[1] } else { $null }; usagePage = ('0x{0:X4}' -f $_.UsagePage); usage = ('0x{0:X4}' -f $_.Usage); inputBytes = $_.InputBytes; outputBytes = $_.OutputBytes; featureBytes = $_.FeatureBytes; inputReportIds = $_.InputReportIds; outputReportIds = $_.OutputReportIds; featureReportIds = $_.FeatureReportIds; error = $_.Error }
})
$sanitized | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $safe "hid-$Label.json") -Encoding UTF8
$sanitized | ConvertTo-Json -Depth 6
