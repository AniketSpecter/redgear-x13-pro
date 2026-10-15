[CmdletBinding()]
param([string]$Label = 'baseline')
$ErrorActionPreference = 'Stop'
if ($Label -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Label must contain only letters, digits, underscores and hyphens.' }
$root = Split-Path $PSScriptRoot -Parent
$raw = Join-Path $root 'evidence/raw'
$safe = Join-Path $root 'evidence/sanitized'
New-Item -ItemType Directory -Force -Path $raw,$safe | Out-Null
$rows = @(Get-PnpDevice -PresentOnly | Where-Object { $_.InstanceId -match '^(HID|USB)\\' } | ForEach-Object {
    $props = @(Get-PnpDeviceProperty -InstanceId $_.InstanceId -ErrorAction SilentlyContinue)
    [pscustomobject]@{ class = $_.Class; name = $_.FriendlyName; status = [string]$_.Status; instanceId = $_.InstanceId; hardwareIds = @($props | Where-Object KeyName -eq 'DEVPKEY_Device_HardwareIds' | ForEach-Object Data); busDescription = @($props | Where-Object KeyName -eq 'DEVPKEY_Device_BusReportedDeviceDesc' | ForEach-Object Data) }
})
$rows | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $raw "pnp-$Label.json") -Encoding UTF8
$rows | ForEach-Object { [ordered]@{ class = $_.class; name = $_.name; status = $_.status; interfaceId = ($_.instanceId -replace '\\[^\\]+$', '\[redacted]'); hardwareIds = $_.hardwareIds; busDescription = $_.busDescription } } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $safe "pnp-$Label.json") -Encoding UTF8
Write-Output ('Saved {0} present PnP interfaces for {1}; no HID reports sent.' -f $rows.Count,$Label)
