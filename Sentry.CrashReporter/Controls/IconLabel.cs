using Windows.ApplicationModel.DataTransfer;
using Sentry.CrashReporter.Extensions;

namespace Sentry.CrashReporter.Controls;

public class IconLabel : StackPanel
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(FrameworkElement), typeof(IconLabel),
            new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty SolidProperty =
        DependencyProperty.Register(nameof(Solid), typeof(string), typeof(IconLabel),
            new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty BrandProperty =
        DependencyProperty.Register(nameof(Brand), typeof(string), typeof(IconLabel),
            new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(IconLabel),
            new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty TextWrappingProperty =
        DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(IconLabel),
            new PropertyMetadata(TextWrapping.NoWrap, OnPropertyChanged));

    public static readonly DependencyProperty IsTextSelectionEnabledProperty =
        DependencyProperty.Register(nameof(IsTextSelectionEnabled), typeof(bool), typeof(IconLabel),
            new PropertyMetadata(true, OnPropertyChanged));

    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(IconLabel),
            new PropertyMetadata(null, OnPropertyChanged));

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

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public bool IsTextSelectionEnabled
    {
        get => (bool)GetValue(IsTextSelectionEnabledProperty);
        set => SetValue(IsTextSelectionEnabledProperty, value);
    }

    public Brush? Foreground
    {
        get => (Brush?)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
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
        Children.Clear();

        var icon = Icon;
        if (icon is null)
        {
            if (Solid is { } solid)
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
            if (icon is IconElement ie && Foreground is not null)
            {
                ie.Foreground = Foreground;
            }
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
            var textBlock = new TextBlock()
                .Text(Text)
                .TextWrapping(TextWrapping)
                .VerticalAlignment(VerticalAlignment.Center)
                .WithTextSelection(IsTextSelectionEnabled);
            if (Foreground is not null)
            {
                textBlock.Foreground(Foreground);
            }
            Children.Add(textBlock);
        }
    }
}
