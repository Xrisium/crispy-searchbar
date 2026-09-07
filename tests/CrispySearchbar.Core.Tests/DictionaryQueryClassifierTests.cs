using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class DictionaryQueryClassifierTests
{
    [Theory]
    [InlineData("apple")]
    [InlineData("search engine")]
    [InlineData("Hello World!")]
    [InlineData("café")]
    [InlineData("123")]
    public void Detect_LatinText_UsesEnglishToChinese(string query)
    {
        Assert.Equal(DictionaryQueryDirection.EnglishToChinese, DictionaryQueryClassifier.Detect(query));
    }

    [Theory]
    [InlineData("苹果")]
    [InlineData("學校")]
    [InlineData("苹果 apple")]
    [InlineData("café 咖啡馆")]
    public void Detect_CjkText_UsesChineseToEnglish(string query)
    {
        Assert.Equal(DictionaryQueryDirection.ChineseToEnglish, DictionaryQueryClassifier.Detect(query));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Detect_EmptyText_DefaultsToEnglishToChinese(string? query)
    {
        Assert.Equal(DictionaryQueryDirection.EnglishToChinese, DictionaryQueryClassifier.Detect(query));
    }
}
