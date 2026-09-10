using CrispySearchbar.Platform;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

public class StartupRegistrationCommandTests
{
    [Fact]
    public void Build_QuotesExecutablePathWithSpaces()
    {
        var command = StartupRegistrationCommand.Build(
            @"C:\Program Files\Crispy Searchbar\CrispySearchbar.exe");

        Assert.Equal(
            @"""C:\Program Files\Crispy Searchbar\CrispySearchbar.exe"" --autostart",
            command);
    }

    [Fact]
    public void Build_DotNetHost_IncludesEntryAssembly()
    {
        var command = StartupRegistrationCommand.Build(
            @"C:\Program Files\dotnet\dotnet.exe",
            @"C:\Apps\Crispy Searchbar\CrispySearchbar.dll");

        Assert.Equal(
            @"""C:\Program Files\dotnet\dotnet.exe"" ""C:\Apps\Crispy Searchbar\CrispySearchbar.dll"" --autostart",
            command);
    }

    [Fact]
    public void Build_EmptyExecutable_ReturnsNull()
    {
        Assert.Null(StartupRegistrationCommand.Build(string.Empty));
    }
}
