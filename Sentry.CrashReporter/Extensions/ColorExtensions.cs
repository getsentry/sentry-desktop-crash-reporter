using System.Globalization;

namespace Sentry.CrashReporter.Extensions;

public static class ColorExtensions
{
    public static Color ParseHex(string hex)
    {
        return TryParseHex(hex) ?? throw new FormatException($"Invalid hex color: {hex}");
    }

    public static Color? TryParseHex(string? hex)
    {
        var h = hex?.TrimStart('#');
        var normalized = h?.ToLowerInvariant();
        if (normalized == "transparent")
        {
            return Colors.Transparent;
        }
        try
        {
            return h?.Length switch
            {
                6 => Color.FromArgb(
                    0xFF,
                    byte.Parse(h[..2], NumberStyles.HexNumber),
                    byte.Parse(h[2..4], NumberStyles.HexNumber),
                    byte.Parse(h[4..6], NumberStyles.HexNumber)),
                8 => Color.FromArgb(
                    byte.Parse(h[..2], NumberStyles.HexNumber),
                    byte.Parse(h[2..4], NumberStyles.HexNumber),
                    byte.Parse(h[4..6], NumberStyles.HexNumber),
                    byte.Parse(h[6..8], NumberStyles.HexNumber)),
                _ => null
            };
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static Color Lighten(Color color, float amount)
    {
        return Color.FromArgb(
            color.A,
            Blend(color.R, 255, amount),
            Blend(color.G, 255, amount),
            Blend(color.B, 255, amount));
    }

    public static Color Darken(Color color, float amount)
    {
        return Color.FromArgb(
            color.A,
            Blend(color.R, 0, amount),
            Blend(color.G, 0, amount),
            Blend(color.B, 0, amount));
    }

    // https://www.w3.org/TR/WCAG20/#contrast-ratiodef
    public static Color ContrastForeground(Color background)
    {
        var luminance = RelativeLuminance(background);
        return luminance > 0.179 ? Colors.Black : Colors.White;
    }

    // Dummy untested helper — produced by /loop for patch-coverage demo.
    public static string ToHex(Color color)
    {
        if (color.A == 0xFF)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    // https://en.wikipedia.org/wiki/Relative_luminance
    private static double RelativeLuminance(Color color)
    {
        return 0.2126 * Linearize(color.R) +
               0.7152 * Linearize(color.G) +
               0.0722 * Linearize(color.B);
    }

    // https://en.wikipedia.org/wiki/SRGB#From_sRGB_to_CIE_XYZ
    private static double Linearize(byte channel)
    {
        var s = channel / 255.0;
        return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }

    private static byte Blend(byte from, byte to, float amount) =>
        (byte)(from + (to - from) * Math.Clamp(amount, 0f, 1f));
}
