using Sentry.CrashReporter.Controls;
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

        this.Content(new ScrollViewer()
            .DataContext(ViewModel, (view, vm) => view 
                .Content(new StackPanel()
                    .Orientation(Orientation.Vertical)
                    .Spacing(8)
                    .Children(new AttachmentGrid()
                        .Data(x => x.Binding(() => vm.Attachments))
                        .OnLaunch(a => _ = ViewModel?.Launch(a))))));
    }
}

internal class AttachmentGrid : Grid
{
    public AttachmentGrid()
    {
        ColumnDefinitions.Add(new ColumnDefinition().Width(new GridLength(1, GridUnitType.Star)));
        ColumnDefinitions.Add(new ColumnDefinition().Width(GridLength.Auto));
        ColumnDefinitions.Add(new ColumnDefinition().Width(GridLength.Auto));

        DataContextChanged += (_, _) => TryAutoBind();
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(
            nameof(Data),
            typeof(List<Attachment>),
            typeof(AttachmentGrid),
            new PropertyMetadata(null, (d, e) =>
            {
                if (d is AttachmentGrid grid)
                {
                    grid.UpdateGrid(e.NewValue as List<Attachment>);
                }
            }));

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

    private void TryAutoBind()
    {
        if (ReadLocalValue(DataProperty) == DependencyProperty.UnsetValue &&
            DataContext is List<Attachment> data)
        {
            Data = data;
        }
    }

    private void UpdateGrid(List<Attachment>? data)
    {
        Children.Clear();
        RowDefinitions.Clear();

        if (data is null)
        {
            return;
        }

        var row = 0;
        var evenBrush = ThemeResource.Get<Brush>("SystemControlTransparentBrush");
        var oddBrush = ThemeResource.Get<Brush>("SystemControlBackgroundListLowBrush");

        foreach (var item in data)
        {
            RowDefinitions.Add(new RowDefinition().Height(GridLength.Auto));

            Children.Add(new Border()
                .Grid(row: row, column: 0)
                .Background(row % 2 == 0 ? evenBrush : oddBrush)
                .CornerRadius(new CornerRadius(2, 0, 0, 2))
                .Padding(new Thickness(4, 2, 8, 2))
                .Child(new TextBlock()
                    .WithTextSelection()
                    .WithSourceCodePro()
                    .Text(item.Filename)));

            Children.Add(new Border()
                .Grid(row: row, column: 1)
                .Background(row % 2 == 0 ? evenBrush : oddBrush)
                .CornerRadius(new CornerRadius(2, 0, 0, 2))
                .Padding(new Thickness(4, 2, 8, 2))
                .Child(new TextBlock()
                    .WithTextSelection()
                    .WithSourceCodePro()
                    .Text(CommunityConverters.ToFileSizeString(item.Data.Length))));

            Children.Add(new Border()
                .Grid(row: row, column: 2)
                .Background(row % 2 == 0 ? evenBrush : oddBrush)
                .CornerRadius(new CornerRadius(0, 2, 2, 0))
                .Padding(new Thickness(8, 2, 4, 2))
                .Child(new Button()
                    .ToolTip("Open")
                    .Content(new FontAwesomeIcon(FA.ArrowUpRightFromSquare).FontSize(12))
                    .Background(Colors.Transparent)
                    .BorderBrush(Colors.Transparent)
                    .Resources(r => r.Add("ButtonBackgroundPointerOver", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBackgroundPressed", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBackgroundDisabled", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBorderBrushPointerOver", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBorderBrushPressed", new SolidColorBrush(Colors.Transparent))
                        .Add("ButtonBorderBrushDisabled", new SolidColorBrush(Colors.Transparent)))
                    .Command(ReactiveCommand.Create(() => Launch?.Invoke(item)))));

            row++;
        }
    }
}
