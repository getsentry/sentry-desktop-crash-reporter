using File = System.IO.File;
using Path = System.IO.Path;

namespace Sentry.CrashReporter.Tests;

public class AppConfigTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_tempDir, true);
    }

    private string WriteConfig(string json, string? subDir = null)
    {
        var dir = subDir is not null ? Path.Combine(_tempDir, subDir) : _tempDir;
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "appsettings.json"), json);
        return dir;
    }

    [Test]
    public void Load_NonExistentDir_ReturnsNull()
    {
        var result = AppConfig.Load("/nonexistent");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Load_NullSearchPath_ReturnsNull()
    {
        var result = AppConfig.Load(null, null);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Load_NoSearchPaths_ReturnsNull()
    {
        var result = AppConfig.Load();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Load_MissingAppConfigSection_ReturnsNull()
    {
        var dir = WriteConfig("""{ "Other": {} }""");

        var result = AppConfig.Load(dir);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Load_InvalidJson_ReturnsNull()
    {
        var dir = WriteConfig("not json");

        var result = AppConfig.Load(dir);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Load_ValidConfig_ReturnsAppConfig()
    {
        var dir = WriteConfig("""
        {
          "AppConfig": {
            "WindowTitle": "Test Title",
            "HeaderText": "Test Header",
            "LogoLight": "light.png",
            "LogoDark": "dark.png",
            "SystemAccentColor": "#FF6600"
          }
        }
        """);

        var result = AppConfig.Load(dir);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.WindowTitle, Is.EqualTo("Test Title"));
        Assert.That(result.HeaderText, Is.EqualTo("Test Header"));
        Assert.That(result.LogoLight, Is.EqualTo("light.png"));
        Assert.That(result.LogoDark, Is.EqualTo("dark.png"));
        Assert.That(result.SystemAccentColor, Is.EqualTo("#FF6600"));
    }

    [Test]
    public void Load_PartialConfig_LeavesOthersNull()
    {
        var dir = WriteConfig("""
        {
          "AppConfig": {
            "WindowTitle": "Only Title"
          }
        }
        """);

        var result = AppConfig.Load(dir);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.WindowTitle, Is.EqualTo("Only Title"));
        Assert.That(result.HeaderText, Is.Null);
        Assert.That(result.HeaderDescription, Is.Null);
        Assert.That(result.CancelButton, Is.Null);
        Assert.That(result.SubmitButton, Is.Null);
        Assert.That(result.WindowClosable, Is.Null);
        Assert.That(result.LogoLight, Is.Null);
        Assert.That(result.LogoDark, Is.Null);
        Assert.That(result.SystemAccentColor, Is.Null);
    }

    [Test]
    public void Load_HeaderDescription_ReturnsValue()
    {
        var dir = WriteConfig("""
        {
          "AppConfig": {
            "HeaderDescription": "Please describe what happened."
          }
        }
        """);

        var result = AppConfig.Load(dir);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.HeaderDescription, Is.EqualTo("Please describe what happened."));
    }

    [Test]
    public void Load_ButtonLabels_ReturnsValues()
    {
        var dir = WriteConfig("""
        {
          "AppConfig": {
            "CancelButton": "Dismiss",
            "SubmitButton": "Send"
          }
        }
        """);

        var result = AppConfig.Load(dir);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CancelButton, Is.EqualTo("Dismiss"));
        Assert.That(result.SubmitButton, Is.EqualTo("Send"));
    }

    [Test]
    public void Load_EmptyCancelButtonText_ReturnsEmptyString()
    {
        var dir = WriteConfig("""
        {
          "AppConfig": {
            "CancelButton": ""
          }
        }
        """);

        var result = AppConfig.Load(dir);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CancelButton, Is.EqualTo(""));
    }

    [Test]
    public void Load_CaseInsensitive_Works()
    {
        var dir = WriteConfig("""
        {
          "AppConfig": {
            "windowtitle": "lower",
            "systemaccentcolor": "#112233"
          }
        }
        """);

        var result = AppConfig.Load(dir);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.WindowTitle, Is.EqualTo("lower"));
        Assert.That(result.SystemAccentColor, Is.EqualTo("#112233"));
    }

    [Test]
    public void Load_FirstMatchWins()
    {
        var first = WriteConfig("""
        {
          "AppConfig": { "WindowTitle": "First" }
        }
        """, "first");

        var second = WriteConfig("""
        {
          "AppConfig": { "WindowTitle": "Second" }
        }
        """, "second");

        var result = AppConfig.Load(first, second);

        Assert.That(result!.WindowTitle, Is.EqualTo("First"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Load_WindowClosable_ReturnsValue(bool closable)
    {
        var dir = WriteConfig($$"""
        {
          "AppConfig": {
            "WindowClosable": {{closable.ToString().ToLowerInvariant()}}
          }
        }
        """);

        var result = AppConfig.Load(dir);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.WindowClosable, Is.EqualTo(closable));
    }

    [Test]
    public void Load_SkipsNullAndMissing_FindsLater()
    {
        var dir = WriteConfig("""
        {
          "AppConfig": { "WindowTitle": "Found" }
        }
        """);

        var result = AppConfig.Load(null, "/nonexistent", dir);

        Assert.That(result!.WindowTitle, Is.EqualTo("Found"));
    }
}
