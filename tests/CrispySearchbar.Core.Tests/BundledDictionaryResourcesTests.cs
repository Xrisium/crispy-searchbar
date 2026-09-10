using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

/// <summary>
/// 内置词典数据必须随程序集内嵌（单文件发布没有外附数据文件），
/// 且流式入口与文件入口的解析结果完全一致。
/// </summary>
public class BundledDictionaryResourcesTests
{
    [Fact]
    public void EmbeddedResources_ExistAndDeriveFormatAndSourceName()
    {
        Assert.True(BundledDictionaryResources.Exists(
            BundledDictionaryResources.CcCedictResourceName));
        Assert.True(BundledDictionaryResources.Exists(
            BundledDictionaryResources.EcdictResourceName));

        Assert.Equal(
            DictionaryFileFormat.Text,
            BundledDictionaryResources.CcCedict.Format);
        Assert.Equal(
            DictionaryFileFormat.Csv,
            BundledDictionaryResources.Ecdict.Format);

        Assert.Equal(
            "cedict_1_0_ts_utf-8_mdbg",
            BundledDictionaryResources.CcCedict.SourceName);
        Assert.Equal(
            "ecdict",
            BundledDictionaryResources.Ecdict.SourceName);

        Assert.False(BundledDictionaryResources.Exists("CrispySearchbar.Core.Data.missing.txt.gz"));
    }

    [Fact]
    public void StreamEntry_MatchesFileEntry()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "crispy-dict-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var gzipPath = Path.Combine(
            directory,
            BundledDictionaryResources.CcCedictFileName + BundledDictionaryResources.GzipExtension);

        try
        {
            using (var source = BundledDictionaryResources.TryOpenCcCedict()!)
            using (var target = File.Create(gzipPath))
            {
                source.CopyTo(target);
            }

            var fromFile = DictionarySourceLoader.LoadChineseIndex(gzipPath);
            CedictIndex fromStream;
            using (var stream = BundledDictionaryResources.TryOpenCcCedict()!)
            {
                fromStream = DictionarySourceLoader.LoadChineseIndex(
                    stream,
                    BundledDictionaryResources.CcCedict.Format,
                    BundledDictionaryResources.CcCedict.SourceName);
            }

            Assert.Equal(fromFile.Count, fromStream.Count);
            Assert.Equal(
                fromFile.Search("苹果")[0].Definitions,
                fromStream.Search("苹果")[0].Definitions);
            Assert.Equal("CC-CEDICT", fromStream.Search("苹果")[0].Source);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
