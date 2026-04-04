namespace Sentry.CrashReporter.Controls;

public static class Toast
{
    private static TeachingTip? _toast;
    private static CancellationTokenSource? _hideCts;

    public static Task Show(
        FrameworkElement element,
        string title,
        string subtitle)
    {
        DependencyObject? current = element;
        while (current is not null)
        {
            if (current is Page { Content: Panel root })
                return Show(root, null, title, subtitle);
            current = VisualTreeHelper.GetParent(current);
        }
        return Task.CompletedTask;
    }

    public static async Task Show(
        Panel parent,
        FrameworkElement? target,
        string title,
        string subtitle,
        TeachingTipPlacementMode placement = TeachingTipPlacementMode.Bottom,
        TimeSpan? duration = null)
    {
        if (_toast is null)
        {
            _toast = new TeachingTip
            {
                IsLightDismissEnabled = false
            };
            parent.Children.Add(_toast);
        }
        else if (!Equals(_toast.Parent, parent))
        {
            (_toast.Parent as Panel)?.Children.Remove(_toast);
            parent.Children.Add(_toast);
        }

        _toast.Title = title;
        _toast.Subtitle = subtitle;
        _toast.PreferredPlacement = placement;
        _toast.IsOpen = true;

        if (target is not null)
        {
            _toast.Target = target;
        }

        // ReSharper disable once MethodHasAsyncOverload
        _hideCts?.Cancel();
        _hideCts = new CancellationTokenSource();
        var token = _hideCts.Token;

        try
        {
            await Task.Delay(duration ?? TimeSpan.FromSeconds(3), token);
            _toast.IsOpen = false;
        }
        catch (OperationCanceledException)
        {
        }
    }

    public static void Hide()
    {
        _hideCts?.Cancel();
        if (_toast is not null)
        {
            _toast.IsOpen = false;
        }
    }
}
