using Windows.ApplicationModel.DataTransfer;

namespace Sentry.CrashReporter.Controls;

public class IconLabel : StackPanel
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(FrameworkElement), typeof(IconLabel),
            new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty SymbolProperty =
        DependencyProperty.Register(nameof(Symbol), typeof(Symbol?), typeof(IconLabel),
            new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty SolidProperty =
        DependencyProperty.Register(nameof(Solid), typeof(string), typeof(IconLabel),
            new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty BrandProperty =
        DependencyProperty.Register(nameof(Brand), typeof(string), typeof(IconLabel),
            new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty ToolTipProperty =
        DependencyProperty.Register(nameof(ToolTip), typeof(string), typeof(IconLabel),
            new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(IconLabel),
            new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty IsTextSelectionEnabledProperty =
        DependencyProperty.Register(nameof(IsTextSelectionEnabled), typeof(bool), typeof(IconLabel),
            new PropertyMetadata(true, OnPropertyChanged));

    public IconLabel(string? icon = null)
    {
        Solid = icon;
        Orientation = Orientation.Horizontal;
        VerticalAlignment = VerticalAlignment.Center;
        Spacing = 8;
        Background = new SolidColorBrush(Colors.Transparent);
        UpdateChildren();
    }

    public FrameworkElement? Icon
    {
        get => (FrameworkElement?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Symbol? Symbol
    {
        get => (Symbol?)GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    public string? Solid
    {
        get => (string?)GetValue(SolidProperty);
        set => SetValue(SolidProperty, value);
    }

    public string? Brand
    {
        get => (string?)GetValue(BrandProperty);
        set => SetValue(BrandProperty, value);
    }

    public string? ToolTip
    {
        get => (string?)GetValue(ToolTipProperty);
        set => SetValue(ToolTipProperty, value);
    }

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsTextSelectionEnabled
    {
        get => (bool)GetValue(IsTextSelectionEnabledProperty);
        set => SetValue(IsTextSelectionEnabledProperty, value);
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IconLabel label)
        {
            label.UpdateChildren();
        }
    }

    private void UpdateChildren()
    {
        if (!DispatcherQueue.HasThreadAccess)
        {
            DispatcherQueue.TryEnqueue(UpdateChildren);
            return;
        }

        Children.Clear();

        var icon = Icon;
        if (icon is null)
        {
            if (Symbol is { } symbol)
            {
                icon = new SymbolIcon(symbol);
            }
            else if (Solid is { } solid)
            {
                icon = new FontAwesomeIcon().Solid(solid);
            }
            else if (Brand is { } brand)
            {
                icon = new FontAwesomeIcon().Brand(Brand);
            }
        }

        if (icon is not null)
        {
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            icon.VerticalAlignment = VerticalAlignment.Center;

            icon.PointerPressed += (_, _) =>
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(Text ?? string.Empty);
                Clipboard.SetContent(dataPackage);
                _ = Toast.Show(this, null, "Copied to clipboard", Text ?? string.Empty);
            };
            Children.Add(icon);
        }

        if (Text is not null)
        {
            Children.Add(new SelectableTextBlock()
                .Text(Text)
                .VerticalAlignment(VerticalAlignment.Center)
                .IsTextSelectionEnabled(IsTextSelectionEnabled));
        }

        ToolTipService.SetToolTip(this, ToolTip);
    }
}
