using CommunityToolkit.Mvvm.Input;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;
using CommunityConverters = CommunityToolkit.Common.Converters;

namespace Sentry.CrashReporter.Views;

public class AttachmentView : ReactiveUserControl<AttachmentViewModel>
{
    public static readonly DependencyProperty EnvelopeProperty = DependencyProperty.Register(
        nameof(Envelope), typeof(Envelope), typeof(AttachmentView), new PropertyMetadata(null));

    public Envelope? Envelope
    {
        get => (Envelope)GetValue(EnvelopeProperty);
        set => SetValue(EnvelopeProperty, value);
    }

    public AttachmentView()
    {
        ViewModel = new AttachmentViewModel();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(v => v.Envelope)
                .BindTo(ViewModel, vm => vm.Envelope)
                .DisposeWith(d);
        });

        this.Content(new UserControl()
            .DataContext(ViewModel, (view, vm) => view 
                .Content(new AttachmentGrid()
                    .Data(x => x.Binding(() => vm.Attachments))
                    .OnLaunch(a => _ = ViewModel?.Launch(a)))));
    }
}

internal class AttachmentGrid : DataGrid
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(List<Attachment>), typeof(AttachmentGrid), new PropertyMetadata(null, OnDataChanged));

    public List<Attachment>? Data
    {
        get => (List<Attachment>?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public event Action<Attachment>? Launch;

    public AttachmentGrid OnLaunch(Action<Attachment> handler)
    {
        Launch += handler;
        return this;
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AttachmentGrid grid)
        {
            grid.ItemsSource = e.NewValue as List<Attachment>;
        }
    }

    public AttachmentGrid()
    {
        DataContextChanged += (_, _) => TryAutoBind();
        ActualThemeChanged += OnThemeChanged;
        DoubleTapped += OnDoubleTapped;
        RightTapped += OnRightTapped;

        IsReadOnly = true;
        AutoGenerateColumns = false;
        GridLinesVisibility = DataGridGridLinesVisibility.None;
        HeadersVisibility = DataGridHeadersVisibility.Column;
        SelectionMode = DataGridSelectionMode.Single;
        ItemsSource = Data;

        UpdateAlternatingRowBackground();

        ContextFlyout = new MenuFlyout
        {
            Items =
            {
                new MenuFlyoutItem
                {
                    Text = "Open",
                    Command = new RelayCommand(LaunchSelected)
                }
            }
        };

        Columns.Add(new DataGridTemplateColumn
        {
            Header = "Filename",
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            CellTemplate = new DataTemplate(() =>
                new TextBlock()
                    .WithSourceCodePro()
                    .Margin(new Thickness(8, 0))
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Text(x => x.Binding("Filename")))
        });

        Columns.Add(new DataGridTemplateColumn
        {
            Header = "Size",
            Width = DataGridLength.Auto,
            CellTemplate = new DataTemplate(() =>
                new TextBlock()
                    .WithSourceCodePro()
                    .Margin(new Thickness(8, 0))
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Text(x => x.Binding("Data.Length").Converter(new FileSizeConverter())))
        });
    }

    private void TryAutoBind()
    {
        if (ReadLocalValue(DataProperty) == DependencyProperty.UnsetValue &&
            DataContext is List<Attachment> data)
        {
            Data = data;
        }
    }

    private void OnThemeChanged(FrameworkElement sender, object args)
    {
        UpdateAlternatingRowBackground();
    }

    private void UpdateAlternatingRowBackground()
    {
        if (Application.Current.Resources.TryGetValue("SystemControlBackgroundListLowBrush", out var brush))
        {
            AlternatingRowBackground = (Brush)brush;
        }
    }

    private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: Attachment item } && ItemsSource is List<Attachment> items)
        {
            SelectedIndex = items.IndexOf(item);
        }
    }

    private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        LaunchSelected();
    }

    private void LaunchSelected()
    {
        if (SelectedItem is Attachment attachment)
        {
            Launch?.Invoke(attachment);
        }
    }

    private sealed class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is int length ? CommunityConverters.ToFileSizeString(length) : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotSupportedException();
    }
}
