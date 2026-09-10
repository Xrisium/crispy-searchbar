<#
.SYNOPSIS
  构建便携单文件版：win-x64 自包含单 exe。

.DESCRIPTION
  使用 PortableWinX64 发布配置（见 src/CrispySearchbar/Properties/PublishProfiles/PortableWinX64.pubxml）：
  词典数据与第三方声明/许可证原文全部内嵌进 exe，产物除 exe 本身外不含任何文件，
  首次运行只会在 exe 同目录自动生成 settings.json。

  脚本会校验发布目录确实只有那个 exe，然后按 crispy-searchbar-{version}-win-x64.exe
  复制到 artifacts/ 并打印 SHA-256。

.EXAMPLE
  pwsh ./scripts/publish-portable.ps1
  pwsh ./scripts/publish-portable.ps1 -Version 0.2.0
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src\CrispySearchbar\CrispySearchbar.csproj'

if (-not $Version) {
    [xml]$project = Get-Content -LiteralPath $projectPath
    $Version = @($project.Project.PropertyGroup.Version | Where-Object { $_ })[0]
}

if (-not $Version) {
    throw '无法从 csproj 读取版本号，请使用 -Version 显式指定。'
}

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts'
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$publishDirectory = Join-Path $tempRoot ('crispy-portable-' + [Guid]::NewGuid().ToString('N'))

Write-Host "发布便携单文件版 v$Version ..."
dotnet publish $projectPath -c Release "-p:PublishProfile=PortableWinX64" "-p:Version=$Version" -o $publishDirectory
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish 失败（退出码 $LASTEXITCODE）。"
}

$published = @(Get-ChildItem -LiteralPath $publishDirectory -File)
$executables = @($published | Where-Object { $_.Extension -eq '.exe' })
if ($executables.Count -ne 1 -or $published.Count -ne 1) {
    $names = if ($published.Count -eq 0) { '（空目录）' } else { $published.Name -join ', ' }
    throw "发布目录应只包含一个 exe，实际内容：$names"
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$targetName = "crispy-searchbar-$Version-win-x64.exe"
$targetPath = Join-Path $OutputDirectory $targetName
Copy-Item -LiteralPath $executables[0].FullName -Destination $targetPath -Force

$sizeMb = [math]::Round((Get-Item -LiteralPath $targetPath).Length / 1MB, 1)
$hash = (Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash

# 只清理脚本自己创建的临时发布目录。
if ($publishDirectory.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

Write-Host "产物：$targetPath"
Write-Host "大小：$sizeMb MB"
Write-Host "SHA-256：$hash"
