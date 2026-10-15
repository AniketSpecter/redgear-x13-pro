[CmdletBinding()]
param([string]$Label = 'baseline')
$ErrorActionPreference = 'Stop'
if ($Label -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Invalid label.' }
$root = Split-Path $PSScriptRoot -Parent
$raw = Join-Path $root 'evidence/raw'
New-Item -ItemType Directory -Force -Path $raw | Out-Null
if (-not ('X13HidInventory' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'HidInventory.cs') }
if (-not ('X13ReadFeature3' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'ReadFeature3.cs') }
$devices = @([X13HidInventory]::Enumerate() | Where-Object { $_.VendorId -eq 0x1b80 -and $_.ProductId -eq 0xb412 -and $_.Path -match '(?i)&mi_01&col03#' -and $_.FeatureBytes -eq 40 -and $_.FeatureReportIds -contains 3 })
if ($devices.Count -ne 1) { throw "Expected one matching status collection, got $($devices.Count). No feature request sent." }
$samples = @(foreach ($i in 1..3) {
    $bytes = [X13ReadFeature3]::Read($devices[0].Path)
    [ordered]@{ sample = $i; timestamp = [DateTimeOffset]::Now.ToString('o'); reportId = 3; windowsBufferBytes = 40; hexIncludingReportId = (($bytes | ForEach-Object { $_.ToString('x2') }) -join ''); dpi = $(if($bytes[1] -lt 7){@(400,800,1200,2400,3200,4000,4800)[$bytes[1]]}else{$null}); pollingRateHz = $(if($bytes[2] -lt 4){@(125,250,500,1000)[$bytes[2]]}else{$null}); staticColor = $(if(($bytes[3] -band 0xf1) -eq 0){'#{0:x2}{1:x2}{2:x2}' -f $bytes[5],$bytes[7],$bytes[6]}else{$null}) }
    Start-Sleep -Milliseconds 100
})
$samples | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $raw "feature3-$Label.json") -Encoding UTF8
$samples | ConvertTo-Json -Depth 5
