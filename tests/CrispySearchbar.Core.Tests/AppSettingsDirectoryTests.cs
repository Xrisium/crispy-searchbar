using CrispySearchbar.Core.Configuration;
using Xunit;

namespace CrispySearchbar.Core.Tests;

/// <summary>
/// 便携版把 exe 放进 Program Files 或只读介质时，配置文件必须回退到用户本地数据目录，
/// 否则设置无法保存。
/// </summary>
public class AppSettingsDirectoryTests
{
    [Fact]
    public void ResolveEffectiveDirectory_PrefersWritableProgramDirectory()
        => Assert.Equal(
            @"C:\app",
            AppSettingsStore.ResolveEffectiveDirectory(
                _ => true,
                @"C:\app",
                @"C:\user"));

    [Fact]
    public void ResolveEffectiveDirectory_FallsBackWhenProgramDirectoryIsReadOnly()
        => Assert.Equal(
            @"C:\user",
            AppSettingsStore.ResolveEffectiveDirectory(
                _ => false,
                @"C:\app",
                @"C:\user"));

    [Fact]
    public void IsDirectoryWritable_IsTrueForTempDirectoryAndLeavesNoProbe()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "crispy-settings-" + Guid.NewGuid().ToString("N"));

        try
        {
            Assert.True(AppSettingsStore.IsDirectoryWritable(directory));
            Assert.Empty(Directory.GetFiles(directory));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void IsDirectoryWritable_IsFalseForInvalidPath()
        => Assert.False(AppSettingsStore.IsDirectoryWritable("\0invalid"));

    [Fact]
    public void FallbackDirectory_LivesUnderLocalAppData()
    {
        var localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        Assert.StartsWith(
            localAppData,
            AppSettingsStore.GetFallbackDirectoryPath(),
            StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(
            "CrispySearchbar",
            AppSettingsStore.GetFallbackDirectoryPath(),
            StringComparison.Ordinal);
    }
}
