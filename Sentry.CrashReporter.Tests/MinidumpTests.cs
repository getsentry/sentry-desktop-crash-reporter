namespace Sentry.CrashReporter.Tests;

public class MinidumpTests
{
    [Test]
    public void FromFile()
    {
        // Act
        var minidump = Minidump.FromFile("data/minidump.dmp");
        var exception = minidump.Streams.Select(s => s.Data).OfType<Minidump.ExceptionStream>().FirstOrDefault();

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ExceptionRec.Code, Is.EqualTo(0xc0000005));
    }
}
