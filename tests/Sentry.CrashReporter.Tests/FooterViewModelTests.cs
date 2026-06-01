using Microsoft.Extensions.DependencyInjection;
using System.Reactive.Threading.Tasks;
using Sentry.CrashReporter.Controls;

namespace Sentry.CrashReporter.Tests;

public class FooterViewModelTests
{
    [SetUp]
    public void SetUp()
    {
        var clipboard = new Mock<IClipboardService>();
        var services = new ServiceCollection();
        services.AddSingleton(new AppConfig());
        services.AddSingleton<ICacheService>(new MemoryCacheService());
        services.AddSingleton<IClipboardService>(clipboard.Object);
        App.Services = services.BuildServiceProvider();
    }

    [Test]
    public void Defaults()
    {
        // Arrange
        var mockReporter = MockReporter();
        var mockWindow = new Mock<IWindowService>();

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object);

        // Assert
        Assert.That(viewModel.Dsn, Is.Null.Or.Empty);
        Assert.That(viewModel.EventId, Is.Null.Or.Empty);
        Assert.That(viewModel.ShortEventId, Is.Null.Or.Empty);
        Assert.That(viewModel.StatusText, Is.Null.Or.Empty);
        Assert.That(viewModel.StatusIcons, Is.Empty);
        Assert.That(viewModel.Status, Is.EqualTo(FooterStatus.Empty));
        Assert.That(viewModel.CacheKeep, Is.EqualTo(CacheKeep.Offline));
    }

    [Test]
    public void Init()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject
            {
                ["dsn"] = "https://foo@bar.com/123",
                ["event_id"] = "12345678-90ab-cdef-1234-567890abcdef"
            },
            new List<EnvelopeItem>()
        );
        var mockReporter = MockReporter();
        var mockWindow = new Mock<IWindowService>();

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object)
        {
            Envelope = envelope
        };

        // Assert
        Assert.That(viewModel.Dsn, Is.EqualTo("https://foo@bar.com/123"));
        Assert.That(viewModel.EventId, Is.EqualTo("12345678-90ab-cdef-1234-567890abcdef"));
        Assert.That(viewModel.ShortEventId, Is.EqualTo("12345678"));
        Assert.That(viewModel.StatusText, Is.EqualTo("12345678"));
        Assert.That(viewModel.Status, Is.EqualTo(FooterStatus.Normal));
        Assert.That(viewModel.CanCache, Is.False);
        Assert.That(viewModel.CacheKeep, Is.EqualTo(CacheKeep.Offline));
        Assert.That(viewModel.StatusIcons, Is.Empty);
    }

    [Test]
    public void Init_WithCacheDirAndMinidump_CanCache()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject
            {
                ["dsn"] = "https://foo@bar.com/123",
                ["event_id"] = "12345678-90ab-cdef-1234-567890abcdef",
                ["cache_dir"] = "/tmp/cache"
            },
            [
                new EnvelopeItem(
                    new JsonObject
                    {
                        ["type"] = "attachment",
                        ["attachment_type"] = "event.minidump",
                        ["filename"] = "minidump.dmp"
                    },
                    [0x01])
            ]
        );
        var mockReporter = MockReporter();
        var mockWindow = new Mock<IWindowService>();

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object)
        {
            Envelope = envelope
        };

        // Assert
        Assert.That(viewModel.CanCache, Is.True);
        Assert.That(viewModel.CacheKeep, Is.EqualTo(CacheKeep.Offline));
        Assert.That(viewModel.StatusText, Is.EqualTo("12345678"));
        Assert.That(viewModel.StatusIcons, Is.EqualTo(new[] { FA.FileCircleExclamation }));
    }

    [Test]
    public async Task CacheKeep_LoadsAndPersistsSelection()
    {
        // Arrange
        var cacheKeep = new MemoryCacheService(CacheKeep.Always);
        var mockReporter = MockReporter(cacheKeep);
        var mockWindow = new Mock<IWindowService>();

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object, cacheKeep);

        // Assert
        Assert.That(viewModel.CacheKeep, Is.EqualTo(CacheKeep.Always));
        Assert.That(viewModel.CacheKeepIndex, Is.EqualTo(2));
        Assert.That(cacheKeep.CacheKeep, Is.EqualTo(CacheKeep.Always));

        // Act
        await viewModel.SetCacheKeepCommand.Execute(CacheKeep.None);

        // Assert
        Assert.That(viewModel.CacheKeepIndex, Is.EqualTo(0));
        Assert.That(cacheKeep.CacheKeep, Is.EqualTo(CacheKeep.None));
    }

    [Test]
    public void CacheKeepOverride_DifferentThanDefault_CanReset()
    {
        // Arrange
        var cacheKeep = new MemoryCacheService(CacheKeep.Always);
        var mockReporter = MockReporter(cacheKeep);
        var mockWindow = new Mock<IWindowService>();

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object, cacheKeep);

        // Assert
        Assert.That(viewModel.CacheKeep, Is.EqualTo(CacheKeep.Always));
        Assert.That(viewModel.CanResetCacheKeep, Is.True);
    }

    [Test]
    public void CacheKeepOverride_SameAsDefault_CanReset()
    {
        // Arrange
        var cacheKeep = new MemoryCacheService(CacheKeep.Offline);
        var mockReporter = MockReporter(cacheKeep);
        var mockWindow = new Mock<IWindowService>();

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object, cacheKeep);

        // Assert
        Assert.That(viewModel.CacheKeep, Is.EqualTo(CacheKeep.Offline));
        Assert.That(viewModel.CanResetCacheKeep, Is.True);
    }

    [Test]
    public async Task ResetCacheKeepCommand_RestoresDefault()
    {
        // Arrange
        var cacheKeep = new MemoryCacheService(CacheKeep.Always);
        var mockReporter = MockReporter(cacheKeep);
        var mockWindow = new Mock<IWindowService>();
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object, cacheKeep);

        // Act
        await viewModel.ResetCacheKeepCommand.Execute();

        // Assert
        Assert.That(viewModel.CacheKeep, Is.EqualTo(CacheKeep.Offline));
        Assert.That(viewModel.CacheKeepIndex, Is.EqualTo(1));
        Assert.That(viewModel.CanResetCacheKeep, Is.False);
        Assert.That(cacheKeep.CacheKeep, Is.Null);
    }

    [Test]
    public async Task SetCacheKeepCommand_UpdatesCacheStatusIcon()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject
            {
                ["dsn"] = "https://foo@bar.com/123",
                ["event_id"] = "12345678-90ab-cdef-1234-567890abcdef",
                ["cache_dir"] = "/tmp/cache"
            },
            new List<EnvelopeItem>()
        );
        var cacheKeep = new MemoryCacheService(CacheKeep.Offline);
        var mockReporter = MockReporter(cacheKeep);
        var mockWindow = new Mock<IWindowService>();
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object, cacheKeep)
        {
            Envelope = envelope
        };

        // Act
        await viewModel.SetCacheKeepCommand.Execute(CacheKeep.Always);

        // Assert
        Assert.That(viewModel.CacheKeep, Is.EqualTo(CacheKeep.Always));
        Assert.That(viewModel.CacheKeepIndex, Is.EqualTo(2));
        Assert.That(viewModel.StatusIcons, Is.EqualTo(new[] { FA.FileCircleCheck }));
        Assert.That(cacheKeep.CacheKeep, Is.EqualTo(CacheKeep.Always));
    }

    [Test]
    public void CacheKeepIndex_UpdatesCacheKeep()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject
            {
                ["dsn"] = "https://foo@bar.com/123",
                ["event_id"] = "12345678-90ab-cdef-1234-567890abcdef",
                ["cache_dir"] = "/tmp/cache"
            },
            new List<EnvelopeItem>()
        );
        var cacheKeep = new MemoryCacheService(CacheKeep.Offline);
        var mockReporter = MockReporter(cacheKeep);
        var mockWindow = new Mock<IWindowService>();
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object, cacheKeep)
        {
            Envelope = envelope
        };

        // Act
        viewModel.CacheKeepIndex = 0;

        // Assert
        Assert.That(viewModel.CacheKeep, Is.EqualTo(CacheKeep.None));
        Assert.That(viewModel.StatusIcons, Is.EqualTo(new[] { FA.FileCircleXmark }));
        Assert.That(cacheKeep.CacheKeep, Is.EqualTo(CacheKeep.None));

        // Act
        viewModel.CacheKeepIndex = 2;

        // Assert
        Assert.That(viewModel.CacheKeep, Is.EqualTo(CacheKeep.Always));
        Assert.That(viewModel.StatusIcons, Is.EqualTo(new[] { FA.FileCircleCheck }));
        Assert.That(cacheKeep.CacheKeep, Is.EqualTo(CacheKeep.Always));
    }

    [Test]
    public async Task CopyEventIdCommand_CopiesEventId()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject
            {
                ["dsn"] = "https://foo@bar.com/123",
                ["event_id"] = "12345678-90ab-cdef-1234-567890abcdef"
            },
            new List<EnvelopeItem>()
        );
        var mockReporter = MockReporter();
        var mockWindow = new Mock<IWindowService>();
        var mockClipboard = new Mock<IClipboardService>();
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object, clipboardService: mockClipboard.Object)
        {
            Envelope = envelope
        };

        // Act
        var copied = await viewModel.CopyEventIdCommand.Execute();

        // Assert
        Assert.That(copied, Is.EqualTo("12345678-90ab-cdef-1234-567890abcdef"));
        mockClipboard.Verify(x => x.SetText("12345678-90ab-cdef-1234-567890abcdef"), Times.Once);
    }

    [Test]
    public async Task Status_is_busy_while_submitting()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject { ["dsn"] = "https://foo@bar.com/123" },
            new List<EnvelopeItem>()
        );
        var mockReporter = MockReporter();
        var tcs = new TaskCompletionSource<object?>();
        mockReporter.Setup(x => x.SubmitAsync(envelope, It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);
        var mockWindow = new Mock<IWindowService>();

        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object)
        {
            Envelope = envelope
        };
        await viewModel.SubmitCommand.CanExecute.FirstAsync();

        // Act
        var task = viewModel.SubmitCommand.Execute().ToTask();

        // Assert
        Assert.That(viewModel.Status, Is.EqualTo(FooterStatus.Busy));

        // Cleanup
        tcs.SetResult(null);
        await task;
    }

    [Test]
    public async Task Status_is_error_when_submit_fails()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject { ["dsn"] = "https://foo@bar.com/123" },
            new List<EnvelopeItem>()
        );
        var mockReporter = MockReporter();
        mockReporter.Setup(x => x.SubmitAsync(It.IsAny<Envelope>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failure"));
        var mockWindow = new Mock<IWindowService>();

        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object)
        {
            Envelope = envelope
        };
        await viewModel.SubmitCommand.CanExecute.FirstAsync();

        // Act
        await viewModel.SubmitCommand.Execute();

        // Assert
        Assert.That(viewModel.Status, Is.EqualTo(FooterStatus.Error));
        Assert.That(viewModel.ErrorMessage, Is.EqualTo("Failure"));
    }

    [Test]
    public async Task CannotSubmit()
    {
        // Arrange
        var mockReporter = MockReporter();
        var mockWindow = new Mock<IWindowService>();

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object);
        var canSubmit = await viewModel.SubmitCommand.CanExecute.FirstOrDefaultAsync();

        // Assert
        Assert.That(canSubmit, Is.False);
    }

    [Test]
    public async Task CanSubmit()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject { ["dsn"] = "https://foo@bar.com/123" },
            new List<EnvelopeItem>()
        );
        var mockReporter = MockReporter();
        var mockWindow = new Mock<IWindowService>();

        // Act
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object)
        {
            Envelope = envelope
        };
        var canSubmit = await viewModel.SubmitCommand.CanExecute.FirstOrDefaultAsync();

        // Assert
        Assert.That(canSubmit, Is.True);
    }

    [Test]
    public async Task Submit_ClosesWindow()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject { ["dsn"] = "https://foo@bar.com/123" },
            new List<EnvelopeItem>()
        );
        var mockReporter = MockReporter();
        var mockWindow = new Mock<IWindowService>();

        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object)
        {
            Envelope = envelope
        };
        await viewModel.SubmitCommand.CanExecute.FirstAsync();

        // Act
        await viewModel.SubmitCommand.Execute();

        // Assert
        mockReporter.Verify(x => x.SubmitAsync(envelope, It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()), Times.Once);
        mockWindow.Verify(x => x.Close(), Times.Once);
    }

    [Test]
    public async Task Submit_DisablesWindowCloseWhileSubmitting()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject { ["dsn"] = "https://foo@bar.com/123" },
            new List<EnvelopeItem>()
        );
        var mockReporter = MockReporter();
        var tcs = new TaskCompletionSource<object?>();
        mockReporter.Setup(x => x.SubmitAsync(envelope, It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);
        var mockWindow = new Mock<IWindowService>();

        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object)
        {
            Envelope = envelope
        };
        await viewModel.SubmitCommand.CanExecute.FirstAsync();

        // Act
        var task = viewModel.SubmitCommand.Execute().ToTask();

        // Assert
        mockWindow.Verify(x => x.SetClosable(false), Times.Once);
        mockWindow.Verify(x => x.Close(), Times.Never);

        // Cleanup
        tcs.SetResult(null);
        await task;
    }

    [Test]
    public async Task Submit_ReenablesWindowCloseWhenSubmitFails()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject { ["dsn"] = "https://foo@bar.com/123" },
            new List<EnvelopeItem>()
        );
        var mockReporter = MockReporter();
        mockReporter.Setup(x => x.SubmitAsync(envelope, It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Failure"));
        var mockWindow = new Mock<IWindowService>();

        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object)
        {
            Envelope = envelope
        };
        await viewModel.SubmitCommand.CanExecute.FirstAsync();

        // Act
        await viewModel.SubmitCommand.Execute();

        // Assert
        mockWindow.Verify(x => x.SetClosable(false), Times.Once);
        mockWindow.Verify(x => x.SetClosable(true), Times.Once);
        mockWindow.Verify(x => x.Close(), Times.Never);
    }

    [Test]
    public async Task Cancel_ClosesWindow()
    {
        // Arrange
        var mockReporter = MockReporter();
        var mockWindow = new Mock<IWindowService>();
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object);

        // Act
        await viewModel.CancelCommand.Execute();

        // Assert
        mockReporter.Verify(x => x.SubmitAsync(It.IsAny<Envelope>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()), Times.Never);
        mockReporter.Verify(x => x.CacheAsync(It.IsAny<Envelope>(), It.IsAny<CancellationToken>()), Times.Never);
        mockWindow.Verify(x => x.Close(), Times.Once);
    }

    [Test]
    public async Task Cancel_WithEnvelope_CachesAndClosesWindow()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject { ["dsn"] = "https://foo@bar.com/123" },
            new List<EnvelopeItem>()
        );
        var mockReporter = MockReporter();
        var mockWindow = new Mock<IWindowService>();
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object)
        {
            Envelope = envelope
        };

        // Act
        await viewModel.CancelCommand.Execute();

        // Assert
        mockReporter.Verify(x => x.SubmitAsync(It.IsAny<Envelope>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()), Times.Never);
        mockReporter.Verify(x => x.CacheAsync(envelope, It.IsAny<CancellationToken>()), Times.Once);
        mockWindow.Verify(x => x.Close(), Times.Once);
    }

    [Test]
    public async Task Cancel_CancelsSubmitWithoutClosingWindow()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject { ["dsn"] = "https://foo@bar.com/123" },
            new List<EnvelopeItem>()
        );
        var mockReporter = MockReporter();
        CancellationToken capturedToken = default;
        var tcs = new TaskCompletionSource<object?>();
        mockReporter.Setup(x => x.SubmitAsync(envelope, It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .Callback<Envelope, IProgress<double>?, CancellationToken>((_, _, token) =>
            {
                capturedToken = token;
                token.Register(() => tcs.TrySetCanceled(token));
            })
            .Returns(tcs.Task);
        var mockWindow = new Mock<IWindowService>();
        var viewModel = new FooterViewModel(mockReporter.Object, mockWindow.Object)
        {
            Envelope = envelope
        };
        await viewModel.SubmitCommand.CanExecute.FirstAsync();

        var submitTask = viewModel.SubmitCommand.Execute().ToTask();
        Assert.That(viewModel.IsSubmitting, Is.True);

        // Act
        await viewModel.CancelCommand.Execute();

        // Assert
        Assert.That(capturedToken.CanBeCanceled, Is.True);
        Assert.That(capturedToken.IsCancellationRequested, Is.True);
        mockReporter.Verify(x => x.CacheAsync(It.IsAny<Envelope>(), It.IsAny<CancellationToken>()), Times.Never);
        mockWindow.Verify(x => x.Close(), Times.Never);

        // Cleanup
        try
        {
            await submitTask;
        }
        catch (TaskCanceledException)
        {
            // Expected.
        }
    }

    private static Mock<ICrashReporter> MockReporter(ICacheService? cache = null, AppConfig? config = null)
    {
        cache ??= new MemoryCacheService();
        config ??= new AppConfig();

        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.EffectiveCacheKeep)
            .Returns(() => (cache.CacheKeep ?? config.CacheKeep ?? CacheKeep.Offline).Normalize());
        return mockReporter;
    }
}
