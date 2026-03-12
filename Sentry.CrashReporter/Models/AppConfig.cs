using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml.Media.Imaging;
using Sentry.CrashReporter.Extensions;
using Path = System.IO.Path;

namespace Sentry.CrashReporter.Models;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AppConfig))]
internal partial class AppConfigJsonContext : JsonSerializerContext;

public record AppConfig
{
    public string? Environment { get; init; }
    public string? WindowTitle { get; init; }
    public string? HeaderText { get; init; }
    public string? HeaderDescription { get; init; }
    public string? CancelButton { get; init; }
    public string? SubmitButton { get; init; }
    public string? SystemAccentColor { get; init; }
    public string? SystemAccentColorLight1 { get; init; }
    public string? SystemAccentColorLight2 { get; init; }
    public string? SystemAccentColorLight3 { get; init; }
    public string? SystemAccentColorDark1 { get; init; }
    public string? SystemAccentColorDark2 { get; init; }
    public string? SystemAccentColorDark3 { get; init; }
    public string? AccentButtonForeground { get; init; }
    public string? LogoLight { get; init; }
    public string? LogoDark { get; init; }

    private const string FileName = "appsettings.json";

    public static AppConfig? Load(params string?[] searchPaths)
    {
        foreach (var dir in searchPaths)
        {
            if (string.IsNullOrEmpty(dir))
            {
                continue;
            }
            var path = Path.Combine(dir, FileName);
            if (!File.Exists(path))
            {
                continue;
            }
            try
            {
                using var stream = File.OpenRead(path);
                using var doc = JsonDocument.Parse(stream);
                if (doc.RootElement.TryGetProperty("AppConfig", out var section))
                {
                    return JsonSerializer.Deserialize(section.GetRawText(), AppConfigJsonContext.Default.AppConfig);
                }
            }
            catch (Exception ex)
            {
                // never fail to launch due to custom config parsing
                typeof(AppConfig).Log().LogWarning(ex, "Failed to load config: {Path}", path);
            }
        }
        return null;
    }

    public void Apply(ResourceDictionary resources)
    {
        if (ColorExtensions.TryParseHex(SystemAccentColor) is { } accent)
        {
            resources["SystemAccentColor"] = accent;
            resources["SystemAccentColorLight1"] = ColorExtensions.TryParseHex(SystemAccentColorLight1) ?? ColorExtensions.Lighten(accent, 0.10f);
            resources["SystemAccentColorLight2"] = ColorExtensions.TryParseHex(SystemAccentColorLight2) ?? ColorExtensions.Lighten(accent, 0.20f);
            resources["SystemAccentColorLight3"] = ColorExtensions.TryParseHex(SystemAccentColorLight3) ?? ColorExtensions.Lighten(accent, 0.30f);
            resources["SystemAccentColorDark1"] = ColorExtensions.TryParseHex(SystemAccentColorDark1) ?? ColorExtensions.Darken(accent, 0.10f);
            resources["SystemAccentColorDark2"] = ColorExtensions.TryParseHex(SystemAccentColorDark2) ?? ColorExtensions.Darken(accent, 0.20f);
            resources["SystemAccentColorDark3"] = ColorExtensions.TryParseHex(SystemAccentColorDark3) ?? ColorExtensions.Darken(accent, 0.35f);
            resources["SystemAccentColorBrush"] = new SolidColorBrush(accent);

            var fg = ColorExtensions.TryParseHex(AccentButtonForeground) ?? ColorExtensions.ContrastForeground(accent);
            resources["AccentButtonForeground"] = new SolidColorBrush(fg);
            resources["AccentButtonForegroundPointerOver"] = new SolidColorBrush(fg);
            resources["AccentButtonForegroundPressed"] = new SolidColorBrush(fg) { Opacity = 0.9 };
            resources["AccentButtonForegroundDisabled"] = new SolidColorBrush(fg) { Opacity = 0.75 };
        }

        if (WindowTitle is not null)
        {
            resources["WindowTitle"] = WindowTitle;
        }

        if (HeaderText is not null)
        {
            resources["HeaderText"] = HeaderText;
        }

        if (HeaderDescription is not null)
        {
            resources["HeaderDescription"] = HeaderDescription;
        }

        if (CancelButton is not null)
        {
            resources["CancelButton"] = CancelButton;
        }

        if (SubmitButton is not null)
        {
            resources["SubmitButton"] = SubmitButton;
        }

        ApplyLogoOverride(resources, LogoLight, "Light");
        ApplyLogoOverride(resources, LogoDark, "Dark");
    }

    private static void ApplyLogoOverride(ResourceDictionary resources, string? logoPath, string themeKey)
    {
        if (string.IsNullOrEmpty(logoPath))
        {
            return;
        }

        var fullPath = Path.IsPathRooted(logoPath)
            ? logoPath
            : Path.Combine(AppContext.BaseDirectory, logoPath);

        try
        {
            var imageSource = new BitmapImage(new Uri(fullPath));

            var imagesDict = resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.OriginalString.Contains("Images.xaml") == true);

            if (imagesDict?.ThemeDictionaries.TryGetValue(themeKey, out var themeObj) == true &&
                themeObj is ResourceDictionary themeDict)
            {
                themeDict["AppLogoIcon"] = imageSource;
            }
        }
        catch (Exception ex)
        {
            typeof(AppConfig).Log().LogWarning(ex, "Failed to load {Theme} logo: {Path}", themeKey, fullPath);
        }
    }
}
