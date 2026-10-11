<#
.SYNOPSIS
  构建便携单文件版：win-x64 自包含单 exe。

.DESCRIPTION
  使用 PortableWinX64 发布配置（见 src/CrispySearchbar/Properties/PublishProfiles/PortableWinX64.pubxml）：
  词典数据与第三方声明/许可证原文全部内嵌进 exe，产物除 exe 本身外不含任何文件，
  配置保存在 %APPDATA%\CrispySearchbar\settings.json，程序目录不写入运行时数据。

  脚本会校验发布目录确实只有那个 exe，然后按 crispy-searchbar-{version}-win-x64.exe
  复制到 artifacts/<当前 HEAD 的短 Commit 编号>/ 并打印 SHA-256。
  -OutputDirectory 可指定最终输出目录；相对路径以仓库根目录为基准。
  发布当前工作区内容；需要产物与提交对应时，应先提交改动再运行脚本。

.EXAMPLE
  pwsh ./scripts/publish-portable.ps1
  pwsh ./scripts/publish-portable.ps1 -Version 0.2.0
  pwsh ./scripts/publish-portable.ps1 -OutputDirectory artifacts/manual-review
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
    [xml]$project = Get-Content -LiteralPath $projectPath -Raw
    $Version = @($project.Project.PropertyGroup.Version | Where-Object { $_ })[0]
}

if (-not $Version) {
    throw '无法从 csproj 读取版本号，请使用 -Version 显式指定。'
}

if ($Version -notmatch '^[0-9A-Za-z][0-9A-Za-z.+-]*$') {
    throw '版本号只能包含字母、数字、点、加号和连字符，且必须以字母或数字开头。'
}

if (-not $OutputDirectory) {
    $commit = & git -C $repoRoot rev-parse --short HEAD
    if ($LASTEXITCODE -ne 0 -or "$commit" -notmatch '^[0-9a-f]{4,64}$') {
        throw '无法读取当前 HEAD 的短 Commit 编号，请确认仓库已有提交且 Git 可用。'
    }

    $OutputDirectory = Join-Path $repoRoot "artifacts/$commit"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot $OutputDirectory
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$publishDirectory = Join-Path $tempRoot ('crispy-portable-' + [Guid]::NewGuid().ToString('N'))

try {
    Write-Host "发布便携单文件版 v$Version ..."
    dotnet publish $projectPath -c Release "-p:PublishProfile=PortableWinX64" "-p:Version=$Version" -o $publishDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish 失败（退出码 $LASTEXITCODE）。"
    }

    # 包括隐藏项和子目录，防止把附带资源目录误判为单文件发布。
    $published = @(Get-ChildItem -LiteralPath $publishDirectory -Force)
    if ($published.Count -ne 1 -or $published[0].PSIsContainer -or $published[0].Extension -ne '.exe') {
        $names = if ($published.Count -eq 0) { '（空目录）' } else { $published.Name -join ', ' }
        throw "发布目录应只包含一个 exe，实际内容：$names"
    }

    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    $targetName = "crispy-searchbar-$Version-win-x64.exe"
    $targetPath = Join-Path $OutputDirectory $targetName
    Copy-Item -LiteralPath $published[0].FullName -Destination $targetPath -Force

    $sizeMb = [math]::Round((Get-Item -LiteralPath $targetPath).Length / 1MB, 1)
    $hash = (Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash

    Write-Host "产物：$targetPath"
    Write-Host "大小：$sizeMb MB"
    Write-Host "SHA-256：$hash"
}
finally {
    # 成功或失败都只清理脚本创建的 GUID 临时目录；删除前核对绝对路径的父目录。
    $resolvedPublishDirectory = [System.IO.Path]::GetFullPath($publishDirectory)
    $resolvedParent = [System.IO.Path]::GetDirectoryName($resolvedPublishDirectory)
    if (-not $resolvedParent.Equals($tempRoot.TrimEnd('\', '/'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "拒绝清理临时目录之外的路径：$resolvedPublishDirectory"
    }

    if (Test-Path -LiteralPath $resolvedPublishDirectory) {
        Remove-Item -LiteralPath $resolvedPublishDirectory -Recurse -Force
    }
}
