using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

public sealed class ThemePaletteTests
{
    [AvaloniaFact]
    public async Task PrimaryButtonAndEnabledToggleShareNormalAndHoverColorsWithoutEffects()
    {
        var application = Application.Current!;
        var previousTheme = application.RequestedThemeVariant;
        var window = new SettingsWindow(null, null,
            configurationDirectory: Path.Combine(Path.GetTempPath(), "crispy-theme-" + Guid.NewGuid().ToString("N")));
        try
        {
            window.Show();
            await PumpAsync();
            var button = window.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("primary"));
            var presenter = button.GetVisualDescendants().OfType<ContentPresenter>().First();
            var toggle = window.GetVisualDescendants().OfType<ToggleSwitch>().First();
            toggle.IsChecked = true;
            var track = toggle.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "SwitchKnobBounds");

            foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark, ThemeVariant.Light })
            {
                application.RequestedThemeVariant = theme;
                window.MouseMove(new Point(1, 1));
                await PumpAsync();
                Assert.Equal(Color.Parse("#68768A"), ColorOf(track.Background));
                Assert.Equal(ColorOf(track.Background), ColorOf(presenter.Background));
                Assert.Equal(0, presenter.BoxShadow.Count);

                window.MouseMove(CenterOf(button, window));
                await PumpAsync();
                var buttonHover = ColorOf(presenter.Background);
                Assert.Equal(Color.Parse("#5C6A7E"), buttonHover);
                Assert.Equal(0, presenter.BoxShadow.Count);

                window.MouseDown(CenterOf(button, window), MouseButton.Left);
                await PumpAsync();
                Assert.Equal(Matrix.Identity, button.RenderTransform?.Value ?? Matrix.Identity);
                Assert.Equal(0, presenter.BoxShadow.Count);
                window.MouseMove(new Point(1, 1));
                window.MouseUp(new Point(1, 1), MouseButton.Left);

                // Bring an actual toggle into view before exercising its Fluent hover state.
                toggle.BringIntoView();
                await PumpAsync();
                window.MouseMove(CenterOf(toggle, window));
                await PumpAsync();
                Assert.Equal(buttonHover, ColorOf(track.Background));
            }
        }
        finally
        {
            window.Close();
            application.RequestedThemeVariant = previousTheme;
        }
    }

    [AvaloniaFact]
    public async Task OpenDropdownAndTextSelectionFollowRuntimeThemeWhileLinksStayBlue()
    {
        var application = Application.Current!;
        var previousTheme = application.RequestedThemeVariant;
        var combo = new ComboBox { ItemsSource = new[] { "First", "Second" }, SelectedIndex = 0, Width = 200 };
        var input = new TextBox { Text = "Selection" };
        var link = new HyperlinkButton { Content = "Link" };
        var panel = new StackPanel { Children = { combo, input, link } };
        var window = new Window { Content = panel, Width = 400, Height = 240 };
        try
        {
            window.Show();
            await PumpAsync();
            combo.IsDropDownOpen = true;
            await PumpAsync();
            var popup = combo.GetVisualDescendants().OfType<Popup>().Single();
            var popupBorder = Assert.IsType<Border>(popup.Child);

            foreach (var dark in new[] { false, true, false })
            {
                application.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
                await PumpAsync();
                Assert.Equal(Color.Parse(dark ? "#222226" : "#FBFBFD"), ColorOf(popupBorder.Background));
                Assert.Equal(Color.Parse(dark ? "#5268768A" : "#2968768A"), ColorOf(input.SelectionBrush));
                Assert.Equal(Color.Parse(dark ? "#6C8CFF" : "#3B6EF6"), ColorOf(link.Foreground));
                Assert.Equal(Color.Parse("#68768A"), (Color)window.FindResource("SystemAccentColor")!);
            }
        }
        finally
        {
            combo.IsDropDownOpen = false;
            window.Close();
            application.RequestedThemeVariant = previousTheme;
        }
    }

    private static Color ColorOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static Point CenterOf(Control control, Window window)
        => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    private static async Task PumpAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Dispatcher.UIThread.InvokeAsync(() => { });
    }
}
