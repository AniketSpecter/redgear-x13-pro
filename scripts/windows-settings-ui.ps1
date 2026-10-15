# Compile the native panel from this repository's source. No download or install.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$cache=Join-Path $root '.tools/windows-settings'
New-Item -ItemType Directory -Force -Path $cache | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(!(Test-Path -LiteralPath $compiler)){$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'}
if(!(Test-Path -LiteralPath $compiler)){throw '.NET Framework C# compiler is required.'}
$binary=Join-Path $cache 'X13WindowsSettings.exe'
& $compiler /nologo /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$binary" (Join-Path $PSScriptRoot 'WindowsMouseSettings.cs') (Join-Path $PSScriptRoot 'WindowsMouseSettingsUi.cs')
if($LASTEXITCODE -ne 0){throw 'Native panel compilation failed.'}
# This is the user-facing interactive settings panel, so show its window.
$panel=Start-Process -FilePath $binary -WindowStyle Normal -PassThru
$panel.WaitForExit()
if($panel.ExitCode -ne 0){throw 'Native panel failed.'}
