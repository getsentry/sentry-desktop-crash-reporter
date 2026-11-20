namespace Sentry.CrashReporter.Controls;

// TODO: remove when CommunityToolkit.WinUI.Controls.Segmented supports Uno 7 (https://github.com/CommunityToolkit/Windows/pull/880)

public partial class Segmented : ListView
{
    public Segmented()
    {
        this.SelectionMode(ListViewSelectionMode.Single)
            .ItemsPanel<StackPanel>(panel => panel.Orientation(Orientation.Horizontal));
    }
}

public partial class SegmentedItem : ListViewItem;
