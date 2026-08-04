using System.Globalization;
using ImageMagick;

return GoldenComparisonCommand.Run(args);

internal static class GoldenComparisonCommand
{
    // Magick.NET returns normalized RMSE; keep the CLI threshold on a byte-channel scale.
    private const double ByteScale = byte.MaxValue;

    public static int Run(string[] args)
    {
        try
        {
            var options = Options.Parse(args);
            if (options.Update)
            {
                var copyResult = CopyGolden(options.ActualPath, options.ExpectedPath);
                var copyStatus = FormatCopyResult(copyResult);
                Console.WriteLine(options.StatusOnly
                    ? copyStatus
                    : FormatUpdateLine(options.ExpectedPath, copyStatus));
                return 0;
            }

            if (!File.Exists(options.ExpectedPath))
            {
                Console.Error.WriteLine($"Missing golden: {options.ExpectedPath}");
                Console.Error.WriteLine("Run `make update-goldens` on the target platform to create it.");
                return 2;
            }

            var result = Compare(options);
            Console.WriteLine(
                $"Golden diff: rmse={result.RootMeanSquareError:F3}, tolerance={options.MaxRootMeanSquareError:F3}");

            if (result.Passed)
            {
                return 0;
            }

            Console.Error.WriteLine($"Golden mismatch. Diff written to: {options.DiffPath}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static CopyResult CopyGolden(string actualPath, string expectedPath)
    {
        if (!File.Exists(actualPath))
        {
            throw new FileNotFoundException("Actual screenshot was not found.", actualPath);
        }

        if (File.Exists(expectedPath) && FilesEqual(actualPath, expectedPath))
        {
            return CopyResult.Unchanged;
        }

        var created = !File.Exists(expectedPath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(expectedPath))!);
        File.Copy(actualPath, expectedPath, overwrite: true);
        return created ? CopyResult.Created : CopyResult.Changed;
    }

    private static bool FilesEqual(string leftPath, string rightPath)
    {
        var leftInfo = new FileInfo(leftPath);
        var rightInfo = new FileInfo(rightPath);
        if (leftInfo.FullName == rightInfo.FullName)
        {
            return true;
        }

        if (leftInfo.Length != rightInfo.Length)
        {
            return false;
        }

        return File.ReadAllBytes(leftPath).AsSpan().SequenceEqual(File.ReadAllBytes(rightPath));
    }

    private static string FormatPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var currentDirectory = Directory.GetCurrentDirectory();
        var relativePath = Path.GetRelativePath(currentDirectory, fullPath);

        return relativePath.StartsWith("..", StringComparison.Ordinal) ? fullPath : relativePath;
    }

    private static string FormatCopyResult(CopyResult copyResult) =>
        copyResult switch
        {
            CopyResult.Unchanged => "",
            CopyResult.Created => "CREATED",
            CopyResult.Changed => "CHANGED",
            _ => throw new ArgumentOutOfRangeException(nameof(copyResult), copyResult, null)
        };

    private static string FormatUpdateLine(string expectedPath, string copyStatus)
    {
        var line = $"  {FormatPath(expectedPath)}...";
        return string.IsNullOrWhiteSpace(copyStatus) ? line : $"{line} {copyStatus}";
    }

    private static ComparisonResult Compare(Options options)
    {
        using var expected = LoadImage(options.ExpectedPath);
        using var actual = LoadImage(options.ActualPath);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.DiffPath))!);

        if (expected.Width != actual.Width || expected.Height != actual.Height)
        {
            WriteSizeMismatchDiff(expected, actual, options.DiffPath);
            return new ComparisonResult(false, double.PositiveInfinity);
        }

        ApplyBlur(expected, options.BlurRadius);
        ApplyBlur(actual, options.BlurRadius);

        using var diff = expected.Compare(actual, ErrorMetric.RootMeanSquared, out var normalizedRootMeanSquareError);
        diff.Write(options.DiffPath, MagickFormat.Png);

        var rootMeanSquareError = normalizedRootMeanSquareError * ByteScale;
        return new ComparisonResult(
            rootMeanSquareError <= options.MaxRootMeanSquareError,
            rootMeanSquareError);
    }

    private static MagickImage LoadImage(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Screenshot was not found.", path);
        }

        return new MagickImage(path);
    }

    private static void ApplyBlur(MagickImage image, int blurRadius)
    {
        if (blurRadius <= 0)
        {
            return;
        }

        image.Blur(0, blurRadius);
    }

    private static void WriteSizeMismatchDiff(MagickImage expected, MagickImage actual, string diffPath)
    {
        var width = Math.Max(expected.Width, actual.Width);
        var height = Math.Max(expected.Height, actual.Height);

        using var diff = new MagickImage(MagickColors.Red, width, height);
        diff.Write(diffPath, MagickFormat.Png);
    }
}

internal sealed record Options(
    string ExpectedPath,
    string ActualPath,
    string DiffPath,
    bool Update,
    bool StatusOnly,
    double MaxRootMeanSquareError,
    int BlurRadius)
{
    public static Options Parse(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "compare", StringComparison.OrdinalIgnoreCase))
        {
            args = args[1..];
        }

        string? expected = null;
        string? actual = null;
        string? diff = null;
        var update = false;
        var statusOnly = false;
        var maxRootMeanSquareError = 5.0;
        var blurRadius = 2;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--expected":
                    expected = RequireValue(args, ref i);
                    break;
                case "--actual":
                    actual = RequireValue(args, ref i);
                    break;
                case "--diff":
                    diff = RequireValue(args, ref i);
                    break;
                case "--update":
                    update = true;
                    break;
                case "--status-only":
                    statusOnly = true;
                    break;
                case "--max-rmse":
                    maxRootMeanSquareError = double.Parse(RequireValue(args, ref i), CultureInfo.InvariantCulture);
                    break;
                case "--blur-radius":
                    blurRadius = int.Parse(RequireValue(args, ref i), CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }

        return new Options(
            expected ?? throw new ArgumentException("Missing required argument: --expected"),
            actual ?? throw new ArgumentException("Missing required argument: --actual"),
            diff ?? throw new ArgumentException("Missing required argument: --diff"),
            update,
            statusOnly,
            maxRootMeanSquareError,
            blurRadius);
    }

    private static string RequireValue(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for argument: {args[index]}");
        }

        index++;
        return args[index];
    }
}

internal enum CopyResult
{
    Unchanged,
    Created,
    Changed
}

internal readonly record struct ComparisonResult(bool Passed, double RootMeanSquareError);
