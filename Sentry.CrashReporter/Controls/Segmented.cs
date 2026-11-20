namespace Sentry.CrashReporter.Controls;

// temporary stand-ins for CommunityToolkit.WinUI.Controls.Segmented until it supports Uno 7

public partial class Segmented : ListView
{
    public Segmented()
    {
        this.SelectionMode(ListViewSelectionMode.Single)
            .ItemsPanel<StackPanel>(panel => panel.Orientation(Orientation.Horizontal));
    }
}

public partial class SegmentedItem : ListViewItem;
