using System.Text.Json.Nodes;
using Sentry.CrashReporter.Controls;

namespace Sentry.CrashReporter.Views;

using JsonGridData = IList<KeyValuePair<string, JsonNode>>;

public sealed class JsonGridView : UserControl
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(JsonGridData), typeof(JsonGridView), new PropertyMetadata(null));

    public JsonGridData? Data
    {
        get => (JsonGridData?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public JsonGridView()
    {
        this.Content(new ScrollViewer()
            .DataContext(this)
            .Content(new JsonGrid()
                .Data(x => x.Binding(() => this.Data))));
    }
}
