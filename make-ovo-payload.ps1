# 把本地的 DeepseekOVO 项目打包成 ovo-payload.zip（供 pack.ps1 内嵌进 exe）
#
# 用法：
#   .\make-ovo-payload.ps1 -OvoDir "D:\DeepseekOVO-公开版"
#
# 产物：本仓库上一级目录下的 ovo-payload.zip（约 217 MB）
# 注意：DeepseekOVO 是第三方开源项目，不随本仓库分发，需要你自己下载。

param(
    [Parameter(Mandatory = $true)]
    [string]$OvoDir
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path (Join-Path $OvoDir 'main.js'))) {
    throw "这个目录不像 DeepseekOVO 项目（缺 main.js）：$OvoDir"
}
if (-not (Test-Path (Join-Path $OvoDir 'runtime\node.exe'))) {
    Write-Warning "没找到 runtime\node.exe —— 内嵌后可能无法启动，请确认用的是完整版"
}

$repo = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path (Split-Path $repo -Parent) 'ovo-payload.zip'

if (Test-Path $out) {
    Write-Host "删除旧包：$out"
    Remove-Item $out -Force
}

Write-Host "打包 $OvoDir ..."
Add-Type -AssemblyName System.IO.Compression.FileSystem
$sw = [Diagnostics.Stopwatch]::StartNew()
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $OvoDir, $out,
    [System.IO.Compression.CompressionLevel]::Fastest,
    $false)
Write-Host ("完成：{0}  {1:0.0} MB  {2}s" -f $out, ((Get-Item $out).Length / 1MB), [int]$sw.Elapsed.TotalSeconds)
Write-Host "现在可以运行 .\pack.ps1 生成单文件 exe"
