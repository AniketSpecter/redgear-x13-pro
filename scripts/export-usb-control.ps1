# Offline only. Does not install drivers, start a capture, or send USB reports.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Capture,
    [Parameter(Mandatory=$true)][ValidateRange(0,65535)][int]$Bus,
    [Parameter(Mandatory=$true)][ValidateRange(1,127)][int]$DeviceAddress,
    [string]$Tshark = 'C:\Program Files\Wireshark\tshark.exe',
    [string]$Label = 'control'
)
$ErrorActionPreference = 'Stop'
if ($Label -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Invalid label.' }
if (-not (Test-Path -LiteralPath $Tshark -PathType Leaf)) { throw 'TShark is not installed. Installation requires approval; see research/capture-workflow.md.' }
$inputPath = (Resolve-Path -LiteralPath $Capture).Path
$root = Split-Path $PSScriptRoot -Parent
$raw = Join-Path $root 'evidence/raw'
New-Item -ItemType Directory -Force -Path $raw | Out-Null
$output = Join-Path $raw "usb-$Label.json"
if (Test-Path -LiteralPath $output) { throw 'Output already exists; choose another label.' }
$filter = "usb.bus_id == $Bus && usb.device_address == $DeviceAddress && usb.transfer_type == 0x02"
& $Tshark -r $inputPath -Y $filter -T json -x | Set-Content -LiteralPath $output -Encoding UTF8
if ($LASTEXITCODE -ne 0) { throw 'TShark extraction failed; partial output remains in raw evidence.' }
Write-Output "Saved local control-transfer JSON to $output. Review and sanitize before sharing."
