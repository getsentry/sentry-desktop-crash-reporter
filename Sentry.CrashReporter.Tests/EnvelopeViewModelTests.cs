
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Sentry.CrashReporter.Models;
using Sentry.CrashReporter.Services;
using Sentry.CrashReporter.ViewModels;

namespace Sentry.CrashReporter.Tests;

public class EnvelopeViewModelTests
{
    [Test]
    public void Defaults()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Envelope?>(null));

        // Act
        var viewModel = new EnvelopeViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.Envelope, Is.Null);
    }

    [Test]
    public void Init()
    {
        // Arrange
        var envelope = new Envelope(new JsonObject(), new List<EnvelopeItem>());
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Envelope?>(envelope));

        // Act
        var viewModel = new EnvelopeViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.Envelope, Is.EqualTo(envelope));
    }

    [Test]
    [TestCase("", false)]
    [TestCase("foo.envelope", true)]
    public void CannotLaunch(string filePath, bool expectedCanLaunch)
    {
        // Arrange
        Envelope? envelope = null;
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.FilePath).Returns(filePath);
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(envelope));

        // Act
        var viewModel = new EnvelopeViewModel(mockReporter.Object);
        var canLaunch = viewModel.LaunchCommand.CanExecute.FirstOrDefaultAsync().GetAwaiter().GetResult();

        // Assert
        Assert.That(canLaunch, Is.EqualTo(expectedCanLaunch));
    }

    // TODO: Launch()
}
