using System.Collections;

namespace Sentry.CrashReporter.Converters;

// TODO: remove when CommunityToolkit.WinUI.Converters supports Uno 7 (https://github.com/CommunityToolkit/Windows/pull/880)

public partial class EmptyObjectToObjectConverter : IValueConverter
{
    public object? EmptyValue { get; set; }
    public object? NotEmptyValue { get; set; }

    public object? Convert(object value, Type targetType, object parameter, string language)
        => CheckValueIsEmpty(value) ? EmptyValue : NotEmptyValue;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();

    protected virtual bool CheckValueIsEmpty(object value) => value is null;
}

public partial class StringVisibilityConverter : EmptyObjectToObjectConverter
{
    public StringVisibilityConverter()
    {
        EmptyValue = Visibility.Collapsed;
        NotEmptyValue = Visibility.Visible;
    }

    protected override bool CheckValueIsEmpty(object value) => string.IsNullOrEmpty(value?.ToString());
}

public partial class CollectionVisibilityConverter : EmptyObjectToObjectConverter
{
    public CollectionVisibilityConverter()
    {
        EmptyValue = Visibility.Collapsed;
        NotEmptyValue = Visibility.Visible;
    }

    protected override bool CheckValueIsEmpty(object value)
        => value is not IEnumerable collection || !collection.GetEnumerator().MoveNext();
}

public partial class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
