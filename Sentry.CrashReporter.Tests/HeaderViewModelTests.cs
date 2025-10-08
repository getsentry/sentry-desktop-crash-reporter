namespace Sentry.CrashReporter.Tests;

public class HeaderViewModelTests
{
    [Test]
    public void Defaults()
    {
        // Arrange
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Envelope?>(null));

        // Act
        var viewModel = new HeaderViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.EventId, Is.Null.Or.Empty);
        Assert.That(viewModel.Timestamp, Is.Null.Or.Empty);
        Assert.That(viewModel.Platform, Is.Null.Or.Empty);
        Assert.That(viewModel.Level, Is.Null.Or.Empty);
        Assert.That(viewModel.Release, Is.Null.Or.Empty);
        Assert.That(viewModel.Environment, Is.Null.Or.Empty);
        Assert.That(viewModel.OsName, Is.Null.Or.Empty);
        Assert.That(viewModel.OsVersion, Is.Null.Or.Empty);
        Assert.That(viewModel.OsPretty, Is.Null.Or.Empty);
        Assert.That(viewModel.Exception, Is.Null.Or.Empty);
    }

    [Test]
    public void Init()
    {
        // Arrange
        var envelope = new Envelope(
            new JsonObject(),
            new List<EnvelopeItem>
            {
                new(new JsonObject { ["type"] = "event" },
                    Encoding.UTF8.GetBytes("{\"event_id\":\"abcdef123456\",\"timestamp\":\"2023-11-23T10:00:00Z\",\"platform\":\"csharp\",\"level\":\"error\",\"release\":\"1.0.0\",\"environment\":\"production\",\"contexts\":{\"os\":{\"name\":\"Windows\",\"version\":\"10.0\"}},\"exception\":{\"values\":[{\"type\":\"System.Exception\",\"value\":\"Test\"}]}}"))
            });
        var mockReporter = new Mock<ICrashReporter>();
        mockReporter.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Envelope?>(envelope));

        // Act
        var viewModel = new HeaderViewModel(mockReporter.Object);

        // Assert
        Assert.That(viewModel.EventId, Is.EqualTo("abcdef12"));
        Assert.That(viewModel.Timestamp, Is.EqualTo(new DateTime(2023, 11, 23, 10, 0, 0, System.DateTimeKind.Utc).ToLocalTime()));
        Assert.That(viewModel.Platform, Is.EqualTo("csharp"));
        Assert.That(viewModel.Level, Is.EqualTo("error"));
        Assert.That(viewModel.Release, Is.EqualTo("1.0.0"));
        Assert.That(viewModel.Environment, Is.EqualTo("production"));
        Assert.That(viewModel.OsName, Is.EqualTo("Windows"));
        Assert.That(viewModel.OsVersion, Is.EqualTo("10.0"));
        Assert.That(viewModel.OsPretty, Is.EqualTo("Windows 10.0"));
        Assert.That(viewModel.Exception, Is.Not.Null);
        Assert.That(viewModel.Exception!.Type, Is.EqualTo("System.Exception"));
        Assert.That(viewModel.Exception!.Value, Is.EqualTo("Test"));
    }
}
