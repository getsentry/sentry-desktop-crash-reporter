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
                CopyGolden(options.ActualPath, options.ExpectedPath);
                Console.WriteLine($"Updated golden: {options.ExpectedPath}");
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

    private static void CopyGolden(string actualPath, string expectedPath)
    {
        if (!File.Exists(actualPath))
        {
            throw new FileNotFoundException("Actual screenshot was not found.", actualPath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(expectedPath))!);
        File.Copy(actualPath, expectedPath, overwrite: true);
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
        var maxRootMeanSquareError = 4.0;
        var blurRadius = 1;

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

internal readonly record struct ComparisonResult(bool Passed, double RootMeanSquareError);
