#if __DESKTOP__
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Sentry.CrashReporter;
using Sentry.CrashReporter.Controls;
using Sentry.CrashReporter.Extensions;
using Sentry.CrashReporter.ViewModels;
using Sentry.CrashReporter.Views;
using SkiaSharp;
using Windows.Graphics;
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
    private const double CaptureRasterizationScale = 1.0;
    private const double RasterizationScaleTolerance = 0.001;
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

#if GOLDEN_TEST
            ConfigureCaptureRoot(root);
            ResizeCaptureWindow(window);
#else
            window.Resize(App.DefaultWindowWidth, App.DefaultWindowHeight);
#endif
            ApplyTheme(root);
            var viewModel = await WaitForMainPageAsync(root);
            SelectView(viewModel);

            await WaitForUiIdleAsync(root);
            root.UpdateLayout();
            ApplyTestFont(root);
            root.UpdateLayout();
            await Task.Delay(RenderSettleDelay);
            await WaitForUiIdleAsync(root);
            root.UpdateLayout();
#if GOLDEN_TEST
            ConfigureCaptureRoot(root);
            ResizeCaptureWindow(window);
            root.UpdateLayout();
            AssertCaptureRasterizationScale(root);
#endif

            var renderer = new RenderTargetBitmap();
            await renderer.RenderAsync(root, App.DefaultWindowWidth, App.DefaultWindowHeight);
#if GOLDEN_TEST
            LogCaptureGeometry(root, renderer);
#endif

            var pixels = WindowsRuntimeBufferExtensions.ToArray(await renderer.GetPixelsAsync());
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

            await using (var stream = File.Create(outputPath))
            {
                using var image = SKImage.FromPixelCopy(
                    new SKImageInfo(renderer.PixelWidth, renderer.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul),
                    pixels);
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

    private static void ResizeCaptureWindow(Window window)
    {
        window.AppWindow.Resize(new SizeInt32 { Width = App.DefaultWindowWidth, Height = App.DefaultWindowHeight });
    }

    private static void AssertCaptureRasterizationScale(FrameworkElement root)
    {
        var rasterizationScale = root.XamlRoot?.RasterizationScale ?? double.NaN;
        if (double.IsNaN(rasterizationScale) ||
            Math.Abs(rasterizationScale - CaptureRasterizationScale) > RasterizationScaleTolerance)
        {
            throw new InvalidOperationException(FormattableString.Invariant(
                $"Golden capture requires rasterization scale 1.0, but the app reported {rasterizationScale:0.###}."));
        }
    }

    private static void LogCaptureGeometry(FrameworkElement root, RenderTargetBitmap renderer)
    {
        var rasterizationScale = root.XamlRoot?.RasterizationScale ?? double.NaN;
        Console.WriteLine(FormattableString.Invariant(
            $"Golden capture: root={root.ActualWidth:0.###}x{root.ActualHeight:0.###}, scale={rasterizationScale:0.###}, renderer={renderer.PixelWidth}x{renderer.PixelHeight}"));
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
