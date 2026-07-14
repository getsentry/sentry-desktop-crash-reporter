namespace Sentry.CrashReporter.Tests;

using Microsoft.Extensions.DependencyInjection;
using Path = System.IO.Path;

public class CrashReporterTests
{
    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new AppConfig());
        services.AddSingleton<ICacheService>(new MemoryCacheService());
        App.Services = services.BuildServiceProvider();
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task LoadAsync(string filePath)
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var file = await StorageFile.GetFileFromPathAsync(filePath);
        var reporter = new Services.CrashReporter(file, client.Object);

        // Act
        var envelope = await reporter.LoadAsync();

        // Assert
        Assert.That(envelope, Is.Not.Null);
        Assert.That(envelope!.FilePath, Is.EqualTo(filePath));
    }

    [Test]
    public async Task LoadAsync_Null()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var reporter = new Services.CrashReporter(null, client.Object);

        // Act
        var envelope = await reporter.LoadAsync();

        // Assert
        Assert.That(envelope, Is.Null);
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitAsync_WithValidEnvelope_CallsSentryClient(string filePath)
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var tempDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var file = await CopyFixtureAsync(filePath, tempDir);
        var reporter = new Services.CrashReporter(file, client.Object);

        try
        {
            // Act
            var envelope = await reporter.LoadAsync();
            await reporter.SubmitAsync(envelope!);

            // Assert
            Assert.That(envelope, Is.Not.Null);
            client.Verify(c => c.SubmitEnvelopeAsync(It.IsAny<string>(),
                envelope!,
                It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Test]
    public async Task CacheAsync_WithCacheDir_CachesEnvelopeAndMinidump()
    {
        // Arrange
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, new Mock<ISentryClient>().Object);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var minidump = new byte[] { 0x01, 0x02, 0x03 };
        var envelope = CreateCrashEnvelope(cacheDir, minidump);

        try
        {
            // Act
            await reporter.CacheAsync(envelope);

            // Assert
            await AssertCachedCrashEnvelope(cacheDir, minidump);
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task CacheAsync_WithCacheDirAndSourceEnvelopeWithoutMinidump_MovesEnvelope()
    {
        // Arrange
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, new Mock<ISentryClient>().Object);
        var rootDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(rootDir, "external", "c993afb6-b4ac-48a6-b61b-2558e601d65d.envelope");
        var sourceSiblingPath = Path.Combine(rootDir, "external", "c993afb6-b4ac-48a6-b61b-2558e601d65d-extra.txt");
        var cacheDir = Path.Combine(rootDir, "cache");
        var envelopePath = Path.Combine(cacheDir, "c993afb6-b4ac-48a6-b61b-2558e601d65d.envelope");
        var cacheSiblingPath = Path.Combine(cacheDir, "c993afb6-b4ac-48a6-b61b-2558e601d65d-extra.txt");
        var envelope = CreateCrashEnvelopeWithoutMinidump(cacheDir);
        await WriteSourceEnvelopeAsync(envelope, sourcePath);
        await File.WriteAllBytesAsync(sourceSiblingPath, [0x04]);
        var sourceBytes = await File.ReadAllBytesAsync(sourcePath);

        try
        {
            // Act
            await reporter.CacheAsync(envelope);

            // Assert
            File.Exists(sourcePath).Should().BeFalse();
            File.Exists(envelopePath).Should().BeTrue();
            (await File.ReadAllBytesAsync(envelopePath)).Should().Equal(sourceBytes);
            File.Exists(sourceSiblingPath).Should().BeFalse();
            File.Exists(cacheSiblingPath).Should().BeTrue();
            (await File.ReadAllBytesAsync(cacheSiblingPath)).Should().Equal([0x04]);
            Directory.GetFiles(cacheDir, "*.dmp").Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(rootDir))
            {
                Directory.Delete(rootDir, true);
            }
        }
    }

    [Test]
    public async Task CacheAsync_WithCacheDirAndSourceEnvelopeWithMinidump_SplitsEnvelopeAndDeletesSource()
    {
        // Arrange
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, new Mock<ISentryClient>().Object);
        var rootDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(rootDir, "external", "c993afb6-b4ac-48a6-b61b-2558e601d65d.envelope");
        var sourceSiblingPath = Path.Combine(rootDir, "external", "c993afb6-b4ac-48a6-b61b-2558e601d65d-extra.txt");
        var cacheDir = Path.Combine(rootDir, "cache");
        var envelope = CreateCrashEnvelope(cacheDir, [0x01, 0x02, 0x03]);
        await WriteSourceEnvelopeAsync(envelope, sourcePath);
        await File.WriteAllBytesAsync(sourceSiblingPath, [0x04]);

        try
        {
            // Act
            await reporter.CacheAsync(envelope);

            // Assert
            File.Exists(sourcePath).Should().BeFalse();
            File.Exists(sourceSiblingPath).Should().BeFalse();
            File.Exists(Path.Combine(cacheDir, Path.GetFileName(sourceSiblingPath))).Should().BeFalse();
            await AssertCachedCrashEnvelope(cacheDir, [0x01, 0x02, 0x03]);
        }
        finally
        {
            if (Directory.Exists(rootDir))
            {
                Directory.Delete(rootDir, true);
            }
        }
    }

    [Test]
    public async Task CacheAsync_WithNoCaching_DoesNotCacheCrashEnvelope()
    {
        // Arrange
        var cacheKeep = new MemoryCacheService(CacheKeep.None);
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, new Mock<ISentryClient>().Object, cacheKeep);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var envelope = CreateCrashEnvelope(cacheDir, [0x01]);

        try
        {
            // Act
            await reporter.CacheAsync(envelope);

            // Assert
            Directory.Exists(cacheDir).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task CacheAsync_WithoutCacheDir_DeletesSourceEnvelope()
    {
        // Arrange
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, new Mock<ISentryClient>().Object);
        var rootDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(rootDir, "external", "c993afb6-b4ac-48a6-b61b-2558e601d65d.envelope");
        var sourceSiblingPath = Path.Combine(rootDir, "external", "c993afb6-b4ac-48a6-b61b-2558e601d65d-extra.txt");
        var envelope = CreateCrashEnvelopeWithoutMinidump();
        await WriteSourceEnvelopeAsync(envelope, sourcePath);
        await File.WriteAllBytesAsync(sourceSiblingPath, [0x04]);

        try
        {
            // Act
            await reporter.CacheAsync(envelope);

            // Assert
            File.Exists(sourcePath).Should().BeFalse();
            File.Exists(sourceSiblingPath).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(rootDir))
            {
                Directory.Delete(rootDir, true);
            }
        }
    }

    [Test]
    public async Task CacheAsync_WithoutCacheDir_WhenSourcePathIsInvalid_DoesNotThrow()
    {
        // Arrange
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, new Mock<ISentryClient>().Object);
        var envelope = CreateCrashEnvelopeWithoutMinidump();
        envelope.FilePath = "invalid\0path";

        // Act
        var action = () => reporter.CacheAsync(envelope);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Test]
    public async Task CacheAsync_WhenEnvelopeWriteFails_DoesNotCreateFinalEnvelopeAndAllowsRetry()
    {
        // Arrange
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, new Mock<ISentryClient>().Object);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var envelope = CreateCrashEnvelopeWithoutMinidump(cacheDir);
        var envelopePath = Path.Combine(cacheDir, "c993afb6-b4ac-48a6-b61b-2558e601d65d.envelope");

        try
        {
            // Act
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await reporter.CacheAsync(envelope, cancellation.Token);

            // Assert
            File.Exists(envelopePath).Should().BeFalse();
            Directory.GetFiles(cacheDir, "*.tmp").Should().BeEmpty();

            await reporter.CacheAsync(envelope);
            File.Exists(envelopePath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task SubmitAsync_WithCacheDir_WhenCrashEnvelopeSubmitFails_CachesCrashEnvelopeAndMinidump()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        client.Setup(c => c.SubmitEnvelopeAsync(It.IsAny<string>(), It.IsAny<Envelope>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("upload failed"));

        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var minidump = new byte[] { 0x01, 0x02, 0x03 };
        var envelope = CreateCrashEnvelope(cacheDir, minidump);

        try
        {
            // Act
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => reporter.SubmitAsync(envelope));

            // Assert
            ex?.Message.Should().Be("upload failed");
            await AssertCachedCrashEnvelope(cacheDir, minidump);
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task SubmitAsync_WithCacheDir_WhenServerRejectsWithStatus_DiscardsWithoutCaching()
    {
        // Arrange - a 4xx/5xx status error (as opposed to a network failure) must not be
        // kept in the offline cache for retry.
        var client = new Mock<ISentryClient>();
        client.Setup(c => c.SubmitEnvelopeAsync(It.IsAny<string>(), It.IsAny<Envelope>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("rejected", null, HttpStatusCode.BadRequest));

        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var minidump = new byte[] { 0x01, 0x02, 0x03 };
        var envelope = CreateCrashEnvelope(cacheDir, minidump);

        try
        {
            // Act - the error still surfaces to the caller...
            Assert.ThrowsAsync<HttpRequestException>(() => reporter.SubmitAsync(envelope));

            // ...but the crash is discarded, not written to the offline cache.
            Directory.Exists(cacheDir).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task CacheAsync_WhenCacheIsFull_EvictsOldestEnvelope()
    {
        // Arrange - fill the offline cache to capacity with dummy envelopes, oldest first.
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, new Mock<ISentryClient>().Object);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cacheDir);

        var oldestPath = Path.Combine(cacheDir, "dummy-000.envelope");
        for (var i = 0; i < Services.CrashReporter.DefaultMaxCachedEnvelopes; i++)
        {
            var path = Path.Combine(cacheDir, $"dummy-{i:D3}.envelope");
            await File.WriteAllTextAsync(path, "{}");
            File.SetLastWriteTimeUtc(path, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i));
        }

        var envelope = CreateCrashEnvelopeWithoutMinidump(cacheDir);
        var newEnvelopePath = Path.Combine(cacheDir, "c993afb6-b4ac-48a6-b61b-2558e601d65d.envelope");

        try
        {
            // Act - caching one more must stay within the cap by evicting the oldest.
            await reporter.CacheAsync(envelope);

            // Assert
            Directory.GetFiles(cacheDir, "*.envelope").Should().HaveCount(Services.CrashReporter.DefaultMaxCachedEnvelopes);
            File.Exists(oldestPath).Should().BeFalse();
            File.Exists(newEnvelopePath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task SubmitAsync_WithCacheKeepAlways_EnforcesEnvelopeCap()
    {
        // Arrange - a successful submit with CacheKeep.Always writes a copy through the
        // private cache overload, which must also honor the cap on the same directory.
        const int cap = 3;
        var reporter = new Services.CrashReporter(
            new Mock<IStorageFile>().Object,
            new Mock<ISentryClient>().Object,
            cache: new MemoryCacheService(CacheKeep.Always),
            config: new AppConfig { MaxCachedEnvelopes = cap });
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cacheDir);

        var oldestPath = Path.Combine(cacheDir, "dummy-0.envelope");
        for (var i = 0; i < cap; i++)
        {
            var path = Path.Combine(cacheDir, $"dummy-{i}.envelope");
            await File.WriteAllTextAsync(path, "{}");
            File.SetLastWriteTimeUtc(path, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i));
        }

        var envelope = CreateCrashEnvelopeWithoutMinidump(cacheDir);
        var newEnvelopePath = Path.Combine(cacheDir, "c993afb6-b4ac-48a6-b61b-2558e601d65d.envelope");

        try
        {
            // Act
            await reporter.SubmitAsync(envelope);

            // Assert - the Always copy is written, but the directory stays within the cap.
            Directory.GetFiles(cacheDir, "*.envelope").Should().HaveCount(cap);
            File.Exists(oldestPath).Should().BeFalse();
            File.Exists(newEnvelopePath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task CacheAsync_WithConfiguredCap_EvictsOldestBeyondConfiguredLimit()
    {
        // Arrange - override the default cap via AppConfig.
        const int cap = 3;
        var reporter = new Services.CrashReporter(
            new Mock<IStorageFile>().Object,
            new Mock<ISentryClient>().Object,
            config: new AppConfig { MaxCachedEnvelopes = cap });
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cacheDir);

        var oldestPath = Path.Combine(cacheDir, "dummy-0.envelope");
        for (var i = 0; i < cap; i++)
        {
            var path = Path.Combine(cacheDir, $"dummy-{i}.envelope");
            await File.WriteAllTextAsync(path, "{}");
            File.SetLastWriteTimeUtc(path, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i));
        }

        var envelope = CreateCrashEnvelopeWithoutMinidump(cacheDir);
        var newEnvelopePath = Path.Combine(cacheDir, "c993afb6-b4ac-48a6-b61b-2558e601d65d.envelope");

        try
        {
            // Act
            await reporter.CacheAsync(envelope);

            // Assert - stays within the configured cap, evicting the oldest.
            Directory.GetFiles(cacheDir, "*.envelope").Should().HaveCount(cap);
            File.Exists(oldestPath).Should().BeFalse();
            File.Exists(newEnvelopePath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task SubmitAsync_WithCacheDir_WhenCrashEnvelopeRateLimited_DiscardsWithoutRetry()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        client.Setup(c => c.SubmitEnvelopeAsync(It.IsAny<string>(), It.IsAny<Envelope>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SubmitResult.RateLimited);

        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var minidump = new byte[] { 0x01, 0x02, 0x03 };
        var envelope = CreateCrashEnvelope(cacheDir, minidump);

        try
        {
            // Act - a rate-limited submit must not throw (so the window can close)...
            await reporter.SubmitAsync(envelope);

            // ...and per the offline-caching spec a 429 is discarded and NOT retried, so the
            // crash is not written to the offline cache (only network failures are retried).
            Directory.Exists(cacheDir).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task SubmitAsync_WithCacheDir_WhenCrashEnvelopeSubmitIsCanceled_CachesCrashEnvelopeAndMinidump()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        client.Setup(c => c.SubmitEnvelopeAsync(It.IsAny<string>(), It.IsAny<Envelope>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("upload canceled"));

        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var minidump = new byte[] { 0x01, 0x02, 0x03 };
        var envelope = CreateCrashEnvelope(cacheDir, minidump);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        try
        {
            // Act
            var ex = Assert.ThrowsAsync<OperationCanceledException>(() =>
                reporter.SubmitAsync(envelope, cancellationToken: cancellation.Token));

            // Assert
            ex?.Message.Should().Be("upload canceled");
            await AssertCachedCrashEnvelope(cacheDir, minidump);
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task SubmitAsync_WithCacheDir_WhenCrashEnvelopeSubmitFailsRepeatedly_CachesCrashEnvelopeOnce()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        client.Setup(c => c.SubmitEnvelopeAsync(It.IsAny<string>(), It.IsAny<Envelope>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("send failed"));

        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var envelope = CreateCrashEnvelope(cacheDir, [0x01], eventId: null);

        try
        {
            // Act
            var firstException = Assert.ThrowsAsync<InvalidOperationException>(() => reporter.SubmitAsync(envelope));
            var secondException = Assert.ThrowsAsync<InvalidOperationException>(() => reporter.SubmitAsync(envelope));

            // Assert
            firstException?.Message.Should().Be("send failed");
            secondException?.Message.Should().Be("send failed");
            Directory.GetFiles(cacheDir, "*.envelope").Should().HaveCount(1);
            Directory.GetFiles(cacheDir, "*.dmp").Should().HaveCount(1);
            client.Verify(c => c.SubmitEnvelopeAsync(It.IsAny<string>(), envelope, It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task SubmitAsync_WithCacheDir_WhenCachedCrashEnvelopeSubmitSucceeds_DeletesMinidump()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        client.SetupSequence(c => c.SubmitEnvelopeAsync(
                It.IsAny<string>(),
                It.IsAny<Envelope>(),
                It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("upload failed"))
            .ReturnsAsync(SubmitResult.Delivered);

        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var envelopePath = Path.Combine(cacheDir, "c993afb6-b4ac-48a6-b61b-2558e601d65d.envelope");
        var minidumpPath = Path.Combine(cacheDir, "c993afb6-b4ac-48a6-b61b-2558e601d65d.dmp");
        var envelope = CreateCrashEnvelope(cacheDir, [0x01]);

        try
        {
            // Act
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => reporter.SubmitAsync(envelope));
            await reporter.SubmitAsync(envelope);

            // Assert
            ex?.Message.Should().Be("upload failed");
            File.Exists(envelopePath).Should().BeFalse();
            File.Exists(minidumpPath).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task SubmitAsync_WithCacheKeepAlways_WhenCachedCrashEnvelopeSubmitSucceeds_PreservesCache()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        client.SetupSequence(c => c.SubmitEnvelopeAsync(
                It.IsAny<string>(),
                It.IsAny<Envelope>(),
                It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("upload failed"))
            .ReturnsAsync(SubmitResult.Delivered);

        var cacheKeep = new MemoryCacheService(CacheKeep.Always);
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object, cacheKeep);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var envelope = CreateCrashEnvelope(cacheDir, [0x01]);

        try
        {
            // Act
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => reporter.SubmitAsync(envelope));
            await reporter.SubmitAsync(envelope);

            // Assert
            ex?.Message.Should().Be("upload failed");
            await AssertCachedCrashEnvelope(cacheDir, [0x01]);
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task SubmitAsync_WithCacheDir_WhenCrashEnvelopeSubmitSucceeds_DoesNotCacheCrashEnvelope()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var envelope = CreateCrashEnvelope(cacheDir, [0x01]);

        try
        {
            // Act
            await reporter.SubmitAsync(envelope);

            // Assert
            Directory.Exists(cacheDir).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task SubmitAsync_WithOfflineCache_WhenCrashEnvelopeSubmitSucceeds_DoesNotCacheCrashEnvelope()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var cacheKeep = new MemoryCacheService(CacheKeep.Offline);
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object, cacheKeep);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var envelope = CreateCrashEnvelope(cacheDir, [0x01]);

        try
        {
            // Act
            await reporter.SubmitAsync(envelope);

            // Assert
            Directory.Exists(cacheDir).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task SubmitAsync_WithNoCaching_WhenCrashEnvelopeSubmitSucceeds_DoesNotCacheCrashEnvelope()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var cacheKeep = new MemoryCacheService(CacheKeep.None);
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object, cacheKeep);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var envelope = CreateCrashEnvelope(cacheDir, [0x01]);

        try
        {
            // Act
            await reporter.SubmitAsync(envelope);

            // Assert
            Directory.Exists(cacheDir).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task SubmitAsync_WithCacheKeepAlways_WhenCrashEnvelopeSubmitSucceeds_CachesSplitEnvelope()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var cacheKeep = new MemoryCacheService(CacheKeep.Always);
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object, cacheKeep);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var envelope = CreateCrashEnvelope(cacheDir, [0x01]);

        try
        {
            // Act
            await reporter.SubmitAsync(envelope);

            // Assert
            await AssertCachedCrashEnvelope(cacheDir, [0x01]);
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task CacheAsync_WithSubmittingCrashEnvelope_DoesNotCacheCrashEnvelope()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var submitCompletion = new TaskCompletionSource<SubmitResult>();
        client.Setup(c => c.SubmitEnvelopeAsync(It.IsAny<string>(), It.IsAny<Envelope>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .Returns(submitCompletion.Task);

        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var envelope = CreateCrashEnvelope(cacheDir, [0x01]);
        var submitTask = reporter.SubmitAsync(envelope);

        try
        {
            // Act
            await reporter.CacheAsync(envelope);
            submitCompletion.SetResult(SubmitResult.Delivered);
            await submitTask;

            // Assert
            Directory.Exists(cacheDir).Should().BeFalse();
        }
        finally
        {
            if (!submitCompletion.Task.IsCompleted)
            {
                submitCompletion.SetResult(SubmitResult.Delivered);
                await submitTask;
            }

            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task SubmitAsync_WhenCrashEnvelopeSubmitSucceeds_DeletesSourceEnvelope()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object);
        var rootDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(rootDir, "external", "c993afb6-b4ac-48a6-b61b-2558e601d65d.envelope");
        var cacheSiblingPath = Path.Combine(rootDir, "external", "c993afb6-b4ac-48a6-b61b-2558e601d65d-1.dmp");
        var extraSiblingPath = Path.Combine(rootDir, "external", "c993afb6-b4ac-48a6-b61b-2558e601d65d-extra.txt");
        var nonSiblingPath = Path.Combine(rootDir, "external", "c993afb6-b4ac-48a6-b61b-2558e601d65d.dmp");
        var envelope = CreateCrashEnvelopeWithoutMinidump();
        await WriteSourceEnvelopeAsync(envelope, sourcePath);
        await File.WriteAllBytesAsync(cacheSiblingPath, [0x01]);
        await File.WriteAllBytesAsync(extraSiblingPath, [0x02]);
        await File.WriteAllBytesAsync(nonSiblingPath, [0x03]);

        try
        {
            // Act
            await reporter.SubmitAsync(envelope);

            // Assert
            File.Exists(sourcePath).Should().BeFalse();
            File.Exists(cacheSiblingPath).Should().BeFalse();
            File.Exists(extraSiblingPath).Should().BeFalse();
            File.Exists(nonSiblingPath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(rootDir))
            {
                Directory.Delete(rootDir, true);
            }
        }
    }

    [Test]
    public async Task CacheAsync_WithSubmittedCrashEnvelope_DoesNotCacheCrashEnvelope()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object);
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var envelope = CreateCrashEnvelope(cacheDir, [0x01]);

        try
        {
            // Act
            await reporter.SubmitAsync(envelope);
            await reporter.CacheAsync(envelope);

            // Assert
            Directory.Exists(cacheDir).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    public async Task CacheAsync_WithSubmittedCrashEnvelope_WhenFeedbackSubmitFails_DoesNotCacheCrashEnvelope()
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        client.SetupSequence(c => c.SubmitEnvelopeAsync(
                It.IsAny<string>(),
                It.IsAny<Envelope>(),
                It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SubmitResult.Delivered)
            .ThrowsAsync(new HttpRequestException("feedback failed"));

        var reporter = new Services.CrashReporter(new Mock<IStorageFile>().Object, client.Object);
        reporter.UpdateFeedback(new Feedback("John Doe", "john.doe@example.com", "It crashed!"));
        var cacheDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var envelope = CreateCrashEnvelope(cacheDir, [0x01]);

        try
        {
            // Act
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => reporter.SubmitAsync(envelope));
            await reporter.CacheAsync(envelope);

            // Assert
            ex?.Message.Should().Be("feedback failed");
            Directory.Exists(cacheDir).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
    }

    [Test]
    [TestCase("data/two_items.envelope")]
    public async Task SubmitAsync_WithFeedback_CallsSentryClientTwice(string filePath)
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var tempDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        var file = await CopyFixtureAsync(filePath, tempDir);
        var reporter = new Services.CrashReporter(file, client.Object);
        var submittedEnvelopes = new List<Envelope>();
        client.Setup(c => c.SubmitEnvelopeAsync(It.IsAny<string>(), It.IsAny<Envelope>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .Callback<string, Envelope, IProgress<double>?, CancellationToken>((_, e, _, _) => submittedEnvelopes.Add(e));
        var feedback = new Feedback("John Doe", "john.doe@example.com", "It crashed!");

        try
        {
            // Act
            var envelope = await reporter.LoadAsync();
            reporter.UpdateFeedback(feedback);
            await reporter.SubmitAsync(envelope!);

            // Assert
            Assert.That(envelope, Is.Not.Null);
            Assert.That(submittedEnvelopes, Has.Count.EqualTo(2));
            Assert.That(submittedEnvelopes[0], Is.EqualTo(envelope));
            var feedbackEnvelope = submittedEnvelopes[1];
            Assert.That(feedbackEnvelope.Items, Has.Count.EqualTo(1));
            var feedbackItem = feedbackEnvelope.Items[0];
            Assert.That(feedbackItem.Header["type"]!.GetValue<string>(), Is.EqualTo("feedback"));
            var feedbackJson = JsonNode.Parse(feedbackItem.Payload)!.AsObject();
            var feedbackContext = feedbackJson["contexts"]!["feedback"]!;
            Assert.That(feedbackContext["name"]!.GetValue<string>(), Is.EqualTo(feedback.Name));
            Assert.That(feedbackContext["contact_email"]!.GetValue<string>(), Is.EqualTo(feedback.Email));
            Assert.That(feedbackContext["message"]!.GetValue<string>(), Is.EqualTo(feedback.Message));
            var eventId = envelope!.TryGetEventId();
            Assert.That(feedbackContext["associated_event_id"]!.GetValue<string>(), Is.EqualTo(eventId!.Replace("-", "")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Test]
    [TestCase("data/empty_headers_eof.envelope")]
    public async Task SubmitAsync_NoDsn_Throws(string filePath)
    {
        // Arrange
        var client = new Mock<ISentryClient>();
        var file = await StorageFile.GetFileFromPathAsync(filePath);
        var reporter = new Services.CrashReporter(file, client.Object);

        // Act
        var envelope = await reporter.LoadAsync();
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => reporter.SubmitAsync(envelope!));

        // Assert
        Assert.That(ex?.Message, Does.Match(@"\bDSN\b"));
    }

    private static Envelope CreateCrashEnvelope(
        string cacheDir,
        byte[] minidump,
        string? eventId = "c993afb6b4ac48a6b61b2558e601d65d")
    {
        var header = new JsonObject
        {
            ["dsn"] = "https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42",
            ["cache_dir"] = cacheDir
        };
        if (eventId is not null)
        {
            header["event_id"] = eventId;
        }

        return new Envelope(
            header,
            [
                new EnvelopeItem(
                    new JsonObject { ["type"] = "event" },
                    Encoding.UTF8.GetBytes(new JsonObject { ["message"] = "crashed" }.ToJsonString())),
                new EnvelopeItem(
                    new JsonObject
                    {
                        ["type"] = "attachment",
                        ["length"] = minidump.Length,
                        ["attachment_type"] = "event.minidump",
                        ["filename"] = "minidump.dmp"
                    },
                    minidump)
            ]);
    }

    private static Envelope CreateCrashEnvelopeWithoutMinidump(string? cacheDir = null)
    {
        var header = new JsonObject
        {
            ["dsn"] = "https://e12d836b15bb49d7bbf99e64295d995b:@sentry.io/42",
            ["event_id"] = "c993afb6b4ac48a6b61b2558e601d65d"
        };
        if (cacheDir is not null)
        {
            header["cache_dir"] = cacheDir;
        }

        return new Envelope(
            header,
            [
                new EnvelopeItem(
                    new JsonObject { ["type"] = "event" },
                    Encoding.UTF8.GetBytes(new JsonObject { ["message"] = "crashed" }.ToJsonString()))
            ]);
    }

    private static async Task WriteSourceEnvelopeAsync(Envelope envelope, string sourcePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        await using var stream = File.Create(sourcePath);
        await envelope.SerializeAsync(stream);
        envelope.FilePath = sourcePath;
    }

    private static async Task<StorageFile> CopyFixtureAsync(string filePath, string tempDir)
    {
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir, Path.GetFileName(filePath));
        File.Copy(filePath, tempPath);
        return await StorageFile.GetFileFromPathAsync(tempPath);
    }

    private static async Task AssertCachedCrashEnvelope(string cacheDir, byte[] minidump)
    {
        var envelopePath = Path.Combine(cacheDir, "c993afb6-b4ac-48a6-b61b-2558e601d65d.envelope");
        var minidumpPath = Path.Combine(cacheDir, "c993afb6-b4ac-48a6-b61b-2558e601d65d.dmp");
        File.Exists(envelopePath).Should().BeTrue();
        File.Exists(minidumpPath).Should().BeTrue();
        (await File.ReadAllBytesAsync(minidumpPath)).Should().Equal(minidump);

        await using var file = File.OpenRead(envelopePath);
        var cachedEnvelope = await Envelope.DeserializeAsync(file);
        cachedEnvelope.TryGetEventId().Should().Be("c993afb6-b4ac-48a6-b61b-2558e601d65d");
        cachedEnvelope.Items.Should().HaveCount(1);
        cachedEnvelope.Items.Should().NotContain(i => i.TryGetHeader("attachment_type") == "event.minidump");
        cachedEnvelope.Items.Single().TryGetType().Should().Be("event");
    }
}
