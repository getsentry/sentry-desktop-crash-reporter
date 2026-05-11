using Windows.UI;

namespace Sentry.CrashReporter.Tests;

public class ColorExtensionsTests
{
    [TestCase("#FF6600", 0xFF, 0xFF, 0x66, 0x00)]
    [TestCase("#ff6600", 0xFF, 0xFF, 0x66, 0x00)]
    [TestCase("#8866FF", 0xFF, 0x88, 0x66, 0xFF)]
    [TestCase("#000000", 0xFF, 0x00, 0x00, 0x00)]
    [TestCase("#FFFFFF", 0xFF, 0xFF, 0xFF, 0xFF)]
    public void ParseHex_6Digit_ReturnsColorWithFullAlpha(string hex, int a, int r, int g, int b)
    {
        var color = ColorExtensions.ParseHex(hex);

        Assert.That(color.A, Is.EqualTo(a));
        Assert.That(color.R, Is.EqualTo(r));
        Assert.That(color.G, Is.EqualTo(g));
        Assert.That(color.B, Is.EqualTo(b));
    }

    [TestCase("#80FF6600", 0x80, 0xFF, 0x66, 0x00)]
    [TestCase("#00000000", 0x00, 0x00, 0x00, 0x00)]
    [TestCase("#FFFFFFFF", 0xFF, 0xFF, 0xFF, 0xFF)]
    public void ParseHex_8Digit_ReturnsColorWithAlpha(string hex, int a, int r, int g, int b)
    {
        var color = ColorExtensions.ParseHex(hex);

        Assert.That(color.A, Is.EqualTo(a));
        Assert.That(color.R, Is.EqualTo(r));
        Assert.That(color.G, Is.EqualTo(g));
        Assert.That(color.B, Is.EqualTo(b));
    }

    [TestCase("FF6600")]
    [TestCase("80FF6600")]
    public void ParseHex_WithoutHash_Works(string hex)
    {
        Assert.DoesNotThrow(() => ColorExtensions.ParseHex(hex));
    }

    [TestCase("#FFF")]
    [TestCase("#12345")]
    [TestCase("#GGGGGG")]
    [TestCase("")]
    public void ParseHex_InvalidFormat_ThrowsFormatException(string hex)
    {
        Assert.Throws<FormatException>(() => ColorExtensions.ParseHex(hex));
    }

    [Test]
    public void TryParseHex_Null_ReturnsNull()
    {
        Assert.That(ColorExtensions.TryParseHex(null), Is.Null);
    }

    [TestCase("#FFF")]
    [TestCase("#12345")]
    [TestCase("#GGGGGG")]
    [TestCase("")]
    public void TryParseHex_InvalidFormat_ReturnsNull(string hex)
    {
        Assert.That(ColorExtensions.TryParseHex(hex), Is.Null);
    }

    [Test]
    public void TryParseHex_ValidHex_ReturnsColor()
    {
        Assert.That(ColorExtensions.TryParseHex("#FF6600"), Is.EqualTo(ColorExtensions.ParseHex("#FF6600")));
    }

    [Test]
    public void Lighten_Zero_ReturnsOriginal()
    {
        var color = Color.FromArgb(0xFF, 0x80, 0x40, 0x20);

        var result = ColorExtensions.Lighten(color, 0f);

        Assert.That(result, Is.EqualTo(color));
    }

    [Test]
    public void Lighten_One_ReturnsWhite()
    {
        var color = Color.FromArgb(0xFF, 0x80, 0x40, 0x20);

        var result = ColorExtensions.Lighten(color, 1f);

        Assert.That(result.R, Is.EqualTo(0xFF));
        Assert.That(result.G, Is.EqualTo(0xFF));
        Assert.That(result.B, Is.EqualTo(0xFF));
        Assert.That(result.A, Is.EqualTo(0xFF));
    }

    [Test]
    public void Lighten_Half_BlendsTowardWhite()
    {
        var color = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);

        var result = ColorExtensions.Lighten(color, 0.5f);

        Assert.That(result.R, Is.EqualTo(127));
        Assert.That(result.G, Is.EqualTo(127));
        Assert.That(result.B, Is.EqualTo(127));
    }

    [Test]
    public void Lighten_PreservesAlpha()
    {
        var color = Color.FromArgb(0x80, 0x40, 0x40, 0x40);

        var result = ColorExtensions.Lighten(color, 0.5f);

        Assert.That(result.A, Is.EqualTo(0x80));
    }

    [Test]
    public void Darken_Zero_ReturnsOriginal()
    {
        var color = Color.FromArgb(0xFF, 0x80, 0x40, 0x20);

        var result = ColorExtensions.Darken(color, 0f);

        Assert.That(result, Is.EqualTo(color));
    }

    [Test]
    public void Darken_One_ReturnsBlack()
    {
        var color = Color.FromArgb(0xFF, 0x80, 0x40, 0x20);

        var result = ColorExtensions.Darken(color, 1f);

        Assert.That(result.R, Is.EqualTo(0x00));
        Assert.That(result.G, Is.EqualTo(0x00));
        Assert.That(result.B, Is.EqualTo(0x00));
        Assert.That(result.A, Is.EqualTo(0xFF));
    }

    [Test]
    public void Darken_Half_BlendsTowardBlack()
    {
        var color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

        var result = ColorExtensions.Darken(color, 0.5f);

        Assert.That(result.R, Is.EqualTo(127));
        Assert.That(result.G, Is.EqualTo(127));
        Assert.That(result.B, Is.EqualTo(127));
    }

    [Test]
    public void Darken_PreservesAlpha()
    {
        var color = Color.FromArgb(0x80, 0x40, 0x40, 0x40);

        var result = ColorExtensions.Darken(color, 0.5f);

        Assert.That(result.A, Is.EqualTo(0x80));
    }

    [Test]
    public void Lighten_ClampsAboveOne()
    {
        var color = Color.FromArgb(0xFF, 0x80, 0x80, 0x80);

        var result = ColorExtensions.Lighten(color, 2f);

        Assert.That(result, Is.EqualTo(ColorExtensions.Lighten(color, 1f)));
    }

    [Test]
    public void Darken_ClampsBelowZero()
    {
        var color = Color.FromArgb(0xFF, 0x80, 0x80, 0x80);

        var result = ColorExtensions.Darken(color, -1f);

        Assert.That(result, Is.EqualTo(ColorExtensions.Darken(color, 0f)));
    }

    [Test]
    public void ContrastForeground_DarkBackground_ReturnsWhite()
    {
        var dark = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);

        var result = ColorExtensions.ContrastForeground(dark);

        Assert.That(result, Is.EqualTo(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)));
    }

    [Test]
    public void ContrastForeground_LightBackground_ReturnsBlack()
    {
        var light = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

        var result = ColorExtensions.ContrastForeground(light);

        Assert.That(result, Is.EqualTo(Color.FromArgb(0xFF, 0x00, 0x00, 0x00)));
    }

    [Test]
    public void ContrastForeground_SentryPurple_ReturnsBlack()
    {
        var sentry = ColorExtensions.ParseHex("#8866FF");

        var result = ColorExtensions.ContrastForeground(sentry);

        Assert.That(result, Is.EqualTo(Color.FromArgb(0xFF, 0x00, 0x00, 0x00)));
    }

    [Test]
    public void ContrastForeground_DarkBlue_ReturnsWhite()
    {
        var darkBlue = ColorExtensions.ParseHex("#1B1464");

        var result = ColorExtensions.ContrastForeground(darkBlue);

        Assert.That(result, Is.EqualTo(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)));
    }

    [Test]
    public void ContrastForeground_BrightYellow_ReturnsBlack()
    {
        var yellow = ColorExtensions.ParseHex("#FFFF00");

        var result = ColorExtensions.ContrastForeground(yellow);

        Assert.That(result, Is.EqualTo(Color.FromArgb(0xFF, 0x00, 0x00, 0x00)));
    }
}
