using Windows.UI;
using ColorExtensions = Sentry.CrashReporter.Extensions.ColorExtensions;

namespace Sentry.CrashReporter.RuntimeTests;

[TestClass]
[RunsOnUIThread]
public class AppConfigTests
{
    [TestMethod]
    public void Apply_AccentColor_SetsAccentResources()
    {
        var config = new AppConfig { SystemAccentColor = "#FF6600" };
        var resources = new ResourceDictionary();

        config.Apply(resources);

        var accent = ColorExtensions.ParseHex("#FF6600");
        Assert.AreEqual(accent, resources["SystemAccentColor"]);
        Assert.AreEqual(ColorExtensions.Lighten(accent, 0.10f), resources["SystemAccentColorLight1"]);
        Assert.AreEqual(ColorExtensions.Lighten(accent, 0.20f), resources["SystemAccentColorLight2"]);
        Assert.AreEqual(ColorExtensions.Lighten(accent, 0.30f), resources["SystemAccentColorLight3"]);
        Assert.AreEqual(ColorExtensions.Darken(accent, 0.10f), resources["SystemAccentColorDark1"]);
        Assert.AreEqual(ColorExtensions.Darken(accent, 0.20f), resources["SystemAccentColorDark2"]);
        Assert.AreEqual(ColorExtensions.Darken(accent, 0.35f), resources["SystemAccentColorDark3"]);
    }

    [TestMethod]
    public void Apply_AccentColor_SetsBrush()
    {
        var config = new AppConfig { SystemAccentColor = "#FF6600" };
        var resources = new ResourceDictionary();

        config.Apply(resources);

        var brush = resources["SystemAccentColorBrush"] as SolidColorBrush;
        Assert.IsNotNull(brush);
        Assert.AreEqual(ColorExtensions.ParseHex("#FF6600"), brush!.Color);
    }

    [TestMethod]
    public void Apply_DarkAccentColor_SetsWhiteForeground()
    {
        var config = new AppConfig { SystemAccentColor = "#000000" };
        var resources = new ResourceDictionary();

        config.Apply(resources);

        var fg = (resources["AccentButtonForeground"] as SolidColorBrush)!.Color;
        Assert.AreEqual(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), fg);
    }

    [TestMethod]
    public void Apply_LightAccentColor_SetsBlackForeground()
    {
        var config = new AppConfig { SystemAccentColor = "#FFFF00" };
        var resources = new ResourceDictionary();

        config.Apply(resources);

        var fg = (resources["AccentButtonForeground"] as SolidColorBrush)!.Color;
        Assert.AreEqual(Color.FromArgb(0xFF, 0x00, 0x00, 0x00), fg);
    }

    [TestMethod]
    public void Apply_AccentColor_SetsForegroundOpacities()
    {
        var config = new AppConfig { SystemAccentColor = "#FF6600" };
        var resources = new ResourceDictionary();

        config.Apply(resources);

        var pressed = resources["AccentButtonForegroundPressed"] as SolidColorBrush;
        var disabled = resources["AccentButtonForegroundDisabled"] as SolidColorBrush;
        Assert.AreEqual(0.9, pressed!.Opacity, 0.001);
        Assert.AreEqual(0.75, disabled!.Opacity, 0.001);
    }

    [TestMethod]
    public void Apply_NoAccentColor_DoesNotSetAccentResources()
    {
        var config = new AppConfig { WindowTitle = "Test" };
        var resources = new ResourceDictionary();

        config.Apply(resources);

        Assert.AreEqual(1, resources.Count);
    }

    [TestMethod]
    public void Apply_WindowTitle_SetsResource()
    {
        var config = new AppConfig { WindowTitle = "Custom Title" };
        var resources = new ResourceDictionary();

        config.Apply(resources);

        Assert.AreEqual("Custom Title", resources["WindowTitle"]);
    }

    [TestMethod]
    public void Apply_NoWindowTitle_DoesNotSetResource()
    {
        var config = new AppConfig();
        var resources = new ResourceDictionary
        {
            ["WindowTitle"] = "Original"
        };

        config.Apply(resources);

        Assert.AreEqual("Original", resources["WindowTitle"]);
    }

    [TestMethod]
    public void Apply_HeaderText_SetsResource()
    {
        var config = new AppConfig { HeaderText = "Custom Header" };
        var resources = new ResourceDictionary();

        config.Apply(resources);

        Assert.AreEqual("Custom Header", resources["HeaderText"]);
    }

    [TestMethod]
    public void Apply_NoHeaderText_DoesNotSetResource()
    {
        var config = new AppConfig();
        var resources = new ResourceDictionary
        {
            ["HeaderText"] = "Original"
        };

        config.Apply(resources);

        Assert.AreEqual("Original", resources["HeaderText"]);
    }

    [TestMethod]
    public void Apply_EmptyConfig_DoesNotModifyResources()
    {
        var config = new AppConfig();
        var resources = new ResourceDictionary
        {
            ["WindowTitle"] = "Original",
            ["HeaderText"] = "Original"
        };

        config.Apply(resources);

        Assert.AreEqual(2, resources.Count);
        Assert.AreEqual("Original", resources["WindowTitle"]);
        Assert.AreEqual("Original", resources["HeaderText"]);
    }

    [TestMethod]
    public void Apply_NonExistentLogo_DoesNotThrow()
    {
        var config = new AppConfig { LogoLight = "/nonexistent/logo.png" };
        var resources = new ResourceDictionary();

        config.Apply(resources);
    }
}
