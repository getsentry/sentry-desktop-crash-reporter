using System.Net;

namespace Sentry.CrashReporter.Services;

public enum RateLimitCategory
{
    Any,
    Error,
    Session,
    Transaction,
    Replay,
    Feedback
}

/// <summary>
///     Tracks Sentry rate limits per data category. This is a port of sentry-native's
///     rate limiter (src/sentry_ratelimiter.c): a category that is currently backed off
///     is filtered out of an envelope before it is sent, so telemetry that is still
///     allowed keeps flowing while limited categories are dropped.
/// </summary>
public sealed class RateLimiter(TimeProvider? timeProvider = null)
{
    private static readonly TimeSpan MaxRetryAfter = TimeSpan.FromHours(24);
    private static readonly TimeSpan DefaultRetryAfter = TimeSpan.FromSeconds(60);

    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private readonly Dictionary<RateLimitCategory, DateTimeOffset> _disabledUntil = new();

    /// <summary>
    ///     Maps an envelope item to the rate-limit category it counts against.
    ///     Returns <c>null</c> for items that bypass rate limiting (e.g. client reports).
    /// </summary>
    public static RateLimitCategory? GetCategory(EnvelopeItem item) =>
        item.TryGetType() switch
        {
            "session" => RateLimitCategory.Session,
            "transaction" => RateLimitCategory.Transaction,
            "replay_video" => RateLimitCategory.Replay,
            "feedback" => RateLimitCategory.Feedback,
            "client_report" => null,
            // NOTE: the type here can be "event" or "attachment". Like sentry-native,
            // attachments currently share the ERROR bucket.
            _ => RateLimitCategory.Error
        };

    /// <summary>
    ///     Returns <c>true</c> if the given category is currently backed off.
    /// </summary>
    public bool IsDisabled(RateLimitCategory category)
    {
        var now = _time.GetUtcNow();
        return IsActive(RateLimitCategory.Any, now) || IsActive(category, now);
    }

    /// <summary>
    ///     Updates the limiter from a server response, preferring the
    ///     <c>X-Sentry-Rate-Limits</c> header, then <c>Retry-After</c>, then a bare 429.
    /// </summary>
    public void Update(HttpResponseMessage response)
    {
        // The Sentry-specific X-Sentry-Rate-Limits header always describes rate limits,
        // so honor it on any status. Retry-After / a bare 429 are only rate-limit signals
        // on a 429 - other statuses (notably 503) send Retry-After for unrelated reasons
        // and must not disable every category.
        if (response.Headers.TryGetValues("X-Sentry-Rate-Limits", out var rateLimits))
        {
            UpdateFromHeader(string.Join(',', rateLimits));
        }
        else if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            if (response.Headers.TryGetValues("Retry-After", out var retryAfter))
            {
                UpdateFromRetryAfter(retryAfter.FirstOrDefault());
            }
            else
            {
                Disable(RateLimitCategory.Any, DefaultRetryAfter);
            }
        }
    }

    private bool IsActive(RateLimitCategory category, DateTimeOffset now) =>
        _disabledUntil.TryGetValue(category, out var until) && until > now;

    // Header format: "retry_after:categories:scope:reason, retry_after:categories:..."
    // e.g. "60:error;transaction:organization, 120:session:project"
    private void UpdateFromHeader(string header)
    {
        foreach (var limit in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var fields = limit.Split(':');
            if (!long.TryParse(fields[0], out var seconds))
            {
                continue;
            }

            var retryAfter = RetryAfterFromSeconds(seconds);
            var categories = fields.Length > 1 ? fields[1] : string.Empty;
            if (string.IsNullOrEmpty(categories))
            {
                // No categories means the limit applies to every category.
                Disable(RateLimitCategory.Any, retryAfter);
                continue;
            }

            foreach (var name in categories.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (TryParseCategory(name, out var category))
                {
                    Disable(category, retryAfter);
                }
            }
        }
    }

    // Retry-After only carries a delay in seconds; disables every category.
    private void UpdateFromRetryAfter(string? retryAfter)
    {
        var delay = long.TryParse(retryAfter, out var seconds)
            ? RetryAfterFromSeconds(seconds)
            : DefaultRetryAfter;
        Disable(RateLimitCategory.Any, delay);
    }

    private static bool TryParseCategory(string name, out RateLimitCategory category)
    {
        switch (name)
        {
            case "error":
                category = RateLimitCategory.Error;
                return true;
            case "session":
                category = RateLimitCategory.Session;
                return true;
            case "transaction":
                category = RateLimitCategory.Transaction;
                return true;
            case "replay":
                category = RateLimitCategory.Replay;
                return true;
            case "feedback":
                category = RateLimitCategory.Feedback;
                return true;
            default:
                // Unknown categories are ignored, matching sentry-native.
                category = default;
                return false;
        }
    }

    // Clamp the raw seconds *before* building a TimeSpan: TimeSpan.FromSeconds throws
    // on very large values, so a malformed or hostile header must never reach it
    // unclamped and fail an otherwise successful submit.
    private static TimeSpan RetryAfterFromSeconds(long seconds)
    {
        var maxSeconds = (long)MaxRetryAfter.TotalSeconds;
        if (seconds < 0)
        {
            seconds = 0;
        }
        else if (seconds > maxSeconds)
        {
            seconds = maxSeconds;
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private void Disable(RateLimitCategory category, TimeSpan duration)
    {
        var until = _time.GetUtcNow() + duration;
        // Keep the longest backoff: a later, shorter limit for the same category must
        // not let it resume sending earlier than an existing one already requires.
        if (!_disabledUntil.TryGetValue(category, out var existing) || until > existing)
        {
            _disabledUntil[category] = until;
        }
    }
}
