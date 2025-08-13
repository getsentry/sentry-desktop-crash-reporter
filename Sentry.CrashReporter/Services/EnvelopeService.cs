using System.Diagnostics;

namespace Sentry.CrashReporter.Services;

public interface IEnvelopeService
{
    string FilePath { get; }
    public ValueTask<Envelope?> LoadAsync(CancellationToken cancellationToken = default);
}

public class EnvelopeService(string filePath) : IEnvelopeService
{
    private Envelope? _cachedEnvelope;

    public string FilePath { get; } = filePath;

    public async ValueTask<Envelope?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            return null;
        }

        if (_cachedEnvelope != null)
        {
            return _cachedEnvelope;
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            await using var file = File.OpenRead(FilePath);
            var envelope = await Envelope.DeserializeAsync(file, cancellationToken);
            stopwatch.Stop();
            this.Log().LogInformation($"Loaded {FilePath} in {stopwatch.ElapsedMilliseconds} ms.");
            _cachedEnvelope = envelope;
            return envelope;
        }
        catch (Exception ex)
        {
            this.Log().LogError(ex, $"Failed to load envelope from {FilePath}");
        }

        return null;
    }

    // private async Task<string> ComputeFileHashAsync(CancellationToken cancellationToken)
    // {
    //     await using var stream = File.OpenRead(filePath);
    //     using var sha256 = SHA256.Create();
    //     var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
    //     return Convert.ToHexString(hash);
    // }
}
