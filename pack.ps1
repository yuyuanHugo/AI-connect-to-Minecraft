# Build the single-file launcher (OVO engine only).
# Usage: powershell -ExecutionPolicy Bypass -File .\pack.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$ovoZip = Join-Path (Split-Path $root -Parent) 'ovo-payload.zip'

if (-not (Test-Path $ovoZip)) {
    throw "ovo-payload.zip not found at $ovoZip (it holds the whole DeepseekOVO project)"
}

Write-Host '1/2 compiling exe with embedded resources...'
$sources = Get-ChildItem (Join-Path $root 'src\*.cs') | ForEach-Object { $_.FullName }

$argList = New-Object System.Collections.ArrayList
foreach ($a in @(
    '/nologo', '/target:winexe', '/optimize+',
    "/out:$root\McBotLauncher.exe",
    '/r:System.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll',
    '/r:System.Web.Extensions.dll', '/r:System.Core.dll',
    '/r:System.IO.Compression.dll', '/r:System.IO.Compression.FileSystem.dll',
    "/resource:$ovoZip,McBot.ovo.zip",
    "/resource:$(Join-Path $root 'data\version_manifest_v2.json'),McBot.version_manifest.json",
    "/resource:$(Join-Path $root 'assets\steve.png'),McBot.steve.png",
    "/resource:$(Join-Path $root 'assets\alex.png'),McBot.alex.png"
)) { [void]$argList.Add($a) }

foreach ($s in $sources) { [void]$argList.Add($s) }

& $csc ($argList.ToArray())
if ($LASTEXITCODE -ne 0) { throw "compile failed (exit $LASTEXITCODE)" }

Write-Host '2/2 done'
$exe = Get-Item (Join-Path $root 'McBotLauncher.exe')
Write-Host ("OK -> McBotLauncher.exe  {0:0.0} MB" -f ($exe.Length / 1MB))
