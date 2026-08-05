#if __DESKTOP__
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Sentry.CrashReporter;
using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;
using Sentry.CrashReporter.Views;
using SkiaSharp;
using Path = System.IO.Path;
#endif

namespace Sentry.CrashReporter.Services;

internal static class GoldenTestCapture
{
    private const string OutputPathVariable = "SENTRY_CRASH_REPORTER_GOLDEN_TEST_OUTPUT";
    private const string ThemeVariable = "SENTRY_CRASH_REPORTER_GOLDEN_TEST_THEME";
    private const string ViewVariable = "SENTRY_CRASH_REPORTER_GOLDEN_TEST_VIEW";
    private const string TestFontFamily = "ms-appx:///Assets/Fonts/Ahem/Ahem.ttf#Ahem";
#if GOLDEN_TEST
    private const int BytesPerPixel = 4;
#endif
    private static readonly TimeSpan LoadTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RenderSettleDelay = TimeSpan.FromMilliseconds(500);

    public static bool IsEnabled =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(OutputPathVariable));

    public static async Task CaptureAndExitAsync(Window window)
    {
#if __DESKTOP__
        var outputPath = Environment.GetEnvironmentVariable(OutputPathVariable);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return;
        }

        try
        {
            if (window.Content is not FrameworkElement root)
            {
                throw new InvalidOperationException("The main window has no framework element content to capture.");
            }

            window.Resize(App.DefaultWindowWidth, App.DefaultWindowHeight);
#if GOLDEN_TEST
            ConfigureCaptureRoot(root);
            DisableLayoutRounding(root);
#endif
            ApplyTheme(root);
            var viewModel = await WaitForMainPageAsync(root);
            SelectView(viewModel);

            await WaitForUiIdleAsync(root);
#if GOLDEN_TEST
            ConfigureCaptureRoot(root);
            DisableLayoutRounding(root);
#endif
            root.UpdateLayout();
            ApplyTestFont(root);
#if GOLDEN_TEST
            DisableLayoutRounding(root);
#endif
            root.UpdateLayout();
            await Task.Delay(RenderSettleDelay);
            await WaitForUiIdleAsync(root);
#if GOLDEN_TEST
            ConfigureCaptureRoot(root);
            DisableLayoutRounding(root);
#endif
            root.UpdateLayout();

            var renderer = new RenderTargetBitmap();
#if GOLDEN_TEST
            var capture = GetCaptureGeometry(root);
            using var transform = ApplyCaptureTransform(root, capture.RasterizationScale);
            root.UpdateLayout();
            await renderer.RenderAsync(root, capture.RenderWidth, capture.RenderHeight);
            LogCaptureGeometry(root, renderer, capture);

            var pixels = CropCapturePixels(
                WindowsRuntimeBufferExtensions.ToArray(await renderer.GetPixelsAsync()),
                renderer.PixelWidth,
                renderer.PixelHeight,
                capture.OutputWidth,
                capture.OutputHeight);
            var imageInfo = new SKImageInfo(capture.OutputWidth, capture.OutputHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
#else
            await renderer.RenderAsync(root, App.DefaultWindowWidth, App.DefaultWindowHeight);
            var pixels = WindowsRuntimeBufferExtensions.ToArray(await renderer.GetPixelsAsync());
            var imageInfo = new SKImageInfo(renderer.PixelWidth, renderer.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
#endif

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

            await using (var stream = File.Create(outputPath))
            {
                using var image = SKImage.FromPixelCopy(imageInfo, pixels);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100)
                    ?? throw new InvalidOperationException("Failed to encode golden PNG.");

                data.SaveTo(stream);
                await stream.FlushAsync();
            }

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            Environment.Exit(2);
        }
#else
        await Task.CompletedTask;
#endif
    }

#if __DESKTOP__
    private static void ApplyTheme(FrameworkElement root)
    {
        root.RequestedTheme = Environment.GetEnvironmentVariable(ThemeVariable)?.ToLowerInvariant() switch
        {
            "dark" => ElementTheme.Dark,
            "default" => ElementTheme.Default,
            _ => ElementTheme.Light
        };
    }

#if GOLDEN_TEST
    private static void ConfigureCaptureRoot(FrameworkElement root)
    {
        root.Width = App.DefaultWindowWidth;
        root.Height = App.DefaultWindowHeight;
        root.MinWidth = App.DefaultWindowWidth;
        root.MinHeight = App.DefaultWindowHeight;
        root.MaxWidth = App.DefaultWindowWidth;
        root.MaxHeight = App.DefaultWindowHeight;
    }

    private static void DisableLayoutRounding(DependencyObject root)
    {
        foreach (var element in EnumerateVisualTree(root))
        {
            if (element is UIElement uiElement)
            {
                uiElement.UseLayoutRounding = false;
            }
        }
    }

    private readonly record struct CaptureGeometry(
        double RasterizationScale,
        int RenderWidth,
        int RenderHeight,
        int OutputWidth,
        int OutputHeight);

    private static CaptureGeometry GetCaptureGeometry(FrameworkElement root)
    {
        var rasterizationScale = root.XamlRoot?.RasterizationScale ?? 1.0;
        if (double.IsNaN(rasterizationScale) || rasterizationScale <= 0)
        {
            throw new InvalidOperationException(FormattableString.Invariant(
                $"Golden capture requires a valid rasterization scale, but the app reported {rasterizationScale:0.###}."));
        }

        return new CaptureGeometry(
            rasterizationScale,
            (int)(App.DefaultWindowWidth * rasterizationScale),
            (int)(App.DefaultWindowHeight * rasterizationScale),
            App.DefaultWindowWidth,
            App.DefaultWindowHeight);
    }

    private static IDisposable ApplyCaptureTransform(FrameworkElement root, double rasterizationScale)
    {
        if (Math.Abs(rasterizationScale - 1.0) < double.Epsilon)
        {
            return Disposable.Create(() => { });
        }

        var previousTransform = root.RenderTransform;
        var previousOrigin = root.RenderTransformOrigin;
        root.RenderTransformOrigin = new Point(0, 0);
        root.RenderTransform = new ScaleTransform
        {
            ScaleX = 1.0 / rasterizationScale,
            ScaleY = 1.0 / rasterizationScale
        };

        return Disposable.Create(() =>
        {
            root.RenderTransform = previousTransform;
            root.RenderTransformOrigin = previousOrigin;
        });
    }

    private static byte[] CropCapturePixels(
        byte[] pixels,
        int sourceWidth,
        int sourceHeight,
        int outputWidth,
        int outputHeight)
    {
        if (sourceWidth == outputWidth && sourceHeight == outputHeight)
        {
            return pixels;
        }

        if (sourceWidth < outputWidth || sourceHeight < outputHeight)
        {
            throw new InvalidOperationException(
                $"Golden capture rendered {sourceWidth}x{sourceHeight}, which is smaller than the {outputWidth}x{outputHeight} output.");
        }

        var cropped = new byte[outputWidth * outputHeight * BytesPerPixel];
        var sourceStride = sourceWidth * BytesPerPixel;
        var outputStride = outputWidth * BytesPerPixel;
        for (var y = 0; y < outputHeight; y++)
        {
            Buffer.BlockCopy(pixels, y * sourceStride, cropped, y * outputStride, outputStride);
        }

        return cropped;
    }

    private static void LogCaptureGeometry(FrameworkElement root, RenderTargetBitmap renderer, CaptureGeometry capture)
    {
        Console.WriteLine(FormattableString.Invariant(
            $"Golden capture: root={root.ActualWidth:0.###}x{root.ActualHeight:0.###}, scale={capture.RasterizationScale:0.###}, render={renderer.PixelWidth}x{renderer.PixelHeight}, output={capture.OutputWidth}x{capture.OutputHeight}"));
    }
#endif

    private static void ApplyTestFont(DependencyObject root)
    {
        var fontFamily = new FontFamily(TestFontFamily);
        foreach (var element in EnumerateVisualTree(root))
        {
            switch (element)
            {
                case FontAwesomeIcon:
                case FontIcon:
                    break;
                case TextBlock textBlock:
                    textBlock.FontFamily = fontFamily;
                    break;
                case Control control:
                    control.FontFamily = fontFamily;
                    break;
            }
        }
    }

    private static IEnumerable<DependencyObject> EnumerateVisualTree(DependencyObject element)
    {
        yield return element;

        var children = VisualTreeHelper.GetChildrenCount(element);
        for (var i = 0; i < children; i++)
        {
            foreach (var child in EnumerateVisualTree(VisualTreeHelper.GetChild(element, i)))
            {
                yield return child;
            }
        }
    }

    private static async Task WaitForUiIdleAsync(FrameworkElement root)
    {
        await WaitForDispatcherAsync(root);
        await WaitForDispatcherAsync(root);
    }

    private static Task WaitForDispatcherAsync(FrameworkElement root)
    {
        var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!root.DispatcherQueue.TryEnqueue(() => idle.SetResult()))
        {
            throw new InvalidOperationException("Failed to wait for the UI dispatcher.");
        }

        return idle.Task;
    }

    private static void SelectView(MainViewModel viewModel)
    {
        var viewName = Environment.GetEnvironmentVariable(ViewVariable);
        if (string.IsNullOrWhiteSpace(viewName))
        {
            return;
        }

        var index = Array.FindIndex(MainPage.Views,
            view => string.Equals(view.Region, viewName, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            throw new InvalidOperationException($"Unknown golden view: {viewName}");
        }

        if (!IsViewAvailable(viewName, viewModel))
        {
            throw new InvalidOperationException(
                $"Golden view '{viewName}' is not available for this envelope. Use a fixture that exposes the view.");
        }

        viewModel.SelectedIndex = index;
    }

    private static bool IsViewAvailable(string viewName, MainViewModel viewModel) =>
        viewName.ToLowerInvariant() switch
        {
            "feedback" => true,
            "tags" => viewModel.Tags is { Count: > 0 },
            "contexts" => viewModel.Contexts is { Count: > 0 },
            "extra" => viewModel.Extra is { Count: > 0 },
            "sdk" => viewModel.Sdk is { Count: > 0 },
            "user" => viewModel.User is { Count: > 0 },
            "attachments" => viewModel.Attachments is { Count: > 0 },
            "stacktrace" => viewModel.HasStacktrace,
            "envelope" => true,
            _ => false
        };

    private static async Task<MainViewModel> WaitForMainPageAsync(DependencyObject root)
    {
        var start = Stopwatch.StartNew();
        while (start.Elapsed < LoadTimeout)
        {
            if (FindMainViewModel(root) is { IsExecuting: false } viewModel)
            {
                return viewModel;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Timed out waiting for MainPage to finish loading.");
    }

    private static MainViewModel? FindMainViewModel(DependencyObject element)
    {
        if (element is FrameworkElement { DataContext: MainViewModel vm })
        {
            return vm;
        }

        var children = VisualTreeHelper.GetChildrenCount(element);
        for (var i = 0; i < children; i++)
        {
            if (FindMainViewModel(VisualTreeHelper.GetChild(element, i)) is { } childVm)
            {
                return childVm;
            }
        }

        return null;
    }
#endif
}
