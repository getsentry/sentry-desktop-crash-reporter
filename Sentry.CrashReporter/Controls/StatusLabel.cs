using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Sentry.CrashReporter.Extensions;

namespace Sentry.CrashReporter.Controls;

public sealed class StatusLabel : Border
{
    public static readonly DependencyProperty IconsProperty =
        DependencyProperty.Register(nameof(Icons), typeof(IEnumerable<string>), typeof(StatusLabel),
            new PropertyMetadata(Array.Empty<string>(), OnIconsChanged));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(StatusLabel),
            new PropertyMetadata(string.Empty, OnTextChanged));

    private readonly StackPanel _icons = new()
    {
        Orientation = Orientation.Horizontal,
        VerticalAlignment = VerticalAlignment.Center,
        Spacing = 8
    };

    private readonly TextBlock _text = new TextBlock()
        .VerticalAlignment(VerticalAlignment.Center)
        .WithTextSelection(false);

    public StatusLabel()
    {
        Background = new SolidColorBrush(Colors.Transparent);
        Child = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 8
        }.Children(_icons, _text);

        UpdateIcons();
    }

    public IEnumerable<string> Icons
    {
        get => (IEnumerable<string>)GetValue(IconsProperty);
        set => SetValue(IconsProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    private static void OnIconsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusLabel label)
        {
            label.UpdateIcons();
        }
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusLabel label)
        {
            label._text.Text = e.NewValue as string ?? string.Empty;
        }
    }

    private void UpdateIcons()
    {
        _icons.Children.Clear();
        foreach (var icon in Icons ?? Array.Empty<string>())
        {
            if (string.IsNullOrEmpty(icon))
            {
                continue;
            }
            _icons.Children.Add(new FontAwesomeIcon(icon)
            {
                VerticalAlignment = VerticalAlignment.Center
            });
        }
        _icons.Visibility = _icons.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
