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
                return Show(root, title, subtitle, null);
            current = VisualTreeHelper.GetParent(current);
        }
        return Task.CompletedTask;
    }

    public static async Task Show(
        FrameworkElement context,
        string title,
        string subtitle,
        FrameworkElement? target,
        TeachingTipPlacementMode placement = TeachingTipPlacementMode.Bottom,
        TimeSpan? duration = null)
    {
        var parent = FindParent(context);
        if (parent is null) return;

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

        if (target is not null)
            _toast.Target = target;
        else
            _toast.ClearValue(TeachingTip.TargetProperty);

        // ReSharper disable once MethodHasAsyncOverload
        _hideCts?.Cancel();
        _hideCts = new CancellationTokenSource();
        var token = _hideCts.Token;

        _toast.DispatcherQueue.TryEnqueue(() =>
        {
            if (!token.IsCancellationRequested)
                _toast.IsOpen = true;
        });

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

    private static Panel? FindParent(DependencyObject element)
    {
        Panel? parent = null;
        DependencyObject? current = element;
        while (current is not null)
        {
            if (current is Panel panel)
                parent = panel;
            current = VisualTreeHelper.GetParent(current);
        }
        return parent;
    }
}
