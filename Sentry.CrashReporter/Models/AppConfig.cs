namespace Sentry.CrashReporter.Models;

public record AppConfig
{
    public string? Environment { get; init; }
}