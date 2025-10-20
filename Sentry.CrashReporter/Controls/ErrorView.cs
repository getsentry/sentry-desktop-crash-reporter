using Sentry.CrashReporter.Extensions;

namespace Sentry.CrashReporter.Controls;

public sealed class ErrorView : UserControl
{
    private readonly TextBlock _typeBlock;
    private readonly TextBlock _messageBlock;
    private readonly TextBlock _stackTraceBlock;

    public static readonly DependencyProperty ErrorProperty = DependencyProperty.Register(
        nameof(Error),
        typeof(Exception),
        typeof(ErrorView),
        new PropertyMetadata(null, OnErrorChanged));

    public Exception? Error
    {
        get => (Exception?)GetValue(ErrorProperty);
        set => SetValue(ErrorProperty, value);
    }

    public ErrorView()
    {
        _typeBlock = new TextBlock()
            .WithTextSelection()
            .WithSourceCodePro();

        _messageBlock = new TextBlock()
            .WithTextSelection()
            .WithSourceCodePro()
            .TextWrapping(TextWrapping.Wrap);

        _stackTraceBlock = new TextBlock()
            .WithTextSelection()
            .WithSourceCodePro()
            .TextWrapping(TextWrapping.Wrap);

        Content = new ScrollViewer
        {
            Content = new StackPanel()
                .Spacing(16)
                .Children(
                    _typeBlock,
                    _messageBlock,
                    _stackTraceBlock
                )
        };
    }

    private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ErrorView errorView)
        {
            errorView.UpdateErrorText(e.NewValue as Exception);
        }
    }

    private void UpdateErrorText(Exception? e)
    {
        _typeBlock.Text = e?.GetType().FullName ?? string.Empty;
        _messageBlock.Text = e?.Message ?? string.Empty;
        _stackTraceBlock.Text = e?.StackTrace ?? string.Empty;
    }
}
