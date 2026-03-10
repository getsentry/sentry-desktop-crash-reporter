using System.Globalization;

namespace Sentry.CrashReporter.Extensions;

internal static class ColorExtensions
{
    internal static Color ParseHex(string hex)
    {
        return TryParseHex(hex) ?? throw new FormatException($"Invalid hex color: {hex}");
    }

    internal static Color? TryParseHex(string? hex)
    {
        if (hex is null)
        {
            return null;
        }
        var h = hex.TrimStart('#');
        try
        {
            return h.Length switch
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

    internal static Color Lighten(Color color, float amount)
    {
        return Color.FromArgb(
            color.A,
            Blend(color.R, 255, amount),
            Blend(color.G, 255, amount),
            Blend(color.B, 255, amount));
    }

    internal static Color Darken(Color color, float amount)
    {
        return Color.FromArgb(
            color.A,
            Blend(color.R, 0, amount),
            Blend(color.G, 0, amount),
            Blend(color.B, 0, amount));
    }

    internal static Color ContrastForeground(Color background)
    {
        var luminance = RelativeLuminance(background);
        return luminance > 0.179 ? Black : White;
    }

    private static double RelativeLuminance(Color color)
    {
        return 0.2126 * Linearize(color.R) +
               0.7152 * Linearize(color.G) +
               0.0722 * Linearize(color.B);
    }

    private static double Linearize(byte channel)
    {
        var s = channel / 255.0;
        return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }

    private static byte Blend(byte from, byte to, float amount) =>
        (byte)(from + (to - from) * Math.Clamp(amount, 0f, 1f));

    private static readonly Color White = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
    private static readonly Color Black = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
}
