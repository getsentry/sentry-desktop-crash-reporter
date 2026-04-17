using Windows.Storage.Pickers;

namespace Sentry.CrashReporter.Services;

public interface IFilePickerService
{
    Task<IReadOnlyList<(string Name, byte[] Data)>> PickFilesAsync();
}

public class FilePickerService : IFilePickerService
{
    public async Task<IReadOnlyList<(string Name, byte[] Data)>> PickFilesAsync()
    {
        var picker = new FileOpenPicker { ViewMode = PickerViewMode.List };
        picker.FileTypeFilter.Add("*");

        IReadOnlyList<StorageFile>? files;
        try
        {
            files = await picker.PickMultipleFilesAsync();
        }
        catch (AccessViolationException)
        {
            // TODO: remove once Uno fixes MacOSFileOpenPickerExtension null-pointer deref on cancel.
            return Array.Empty<(string, byte[])>();
        }

        if (files is null || files.Count == 0)
        {
            return Array.Empty<(string, byte[])>();
        }

        var result = new List<(string Name, byte[] Data)>(files.Count);
        foreach (var file in files)
        {
            await using var stream = await file.OpenStreamForReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            result.Add((file.Name, ms.ToArray()));
        }
        return result;
    }
}
