namespace Sentry.CrashReporter.Tests;

public class ExceptionCodeTests
{
    [Test]
    [TestCase(0xc0000005, "EXCEPTION_ACCESS_VIOLATION")]
    [TestCase(0xc000001d, "EXCEPTION_ILLEGAL_INSTRUCTION")]
    [TestCase(0xc00000fd, "EXCEPTION_STACK_OVERFLOW")]
    public void AsExceptionCode_Windows(uint code, string type)
    {
        // Act
        var exceptionCode = code.AsExceptionCode("Windows");

        // Assert
        Assert.That(exceptionCode, Is.Not.Null);
        Assert.That(exceptionCode!.Type, Is.EqualTo(type));
        Assert.That(exceptionCode.Value, Is.Null.Or.Not.Empty);
    }

    [Test]
    [TestCase(0x4u, "SIGILL")]
    [TestCase(0x6u, "SIGABRT")]
    [TestCase(0x8u, "SIGFPE")]
    [TestCase(0xbu, "SIGSEGV")]
    public void AsExceptionCode_Linux(uint code, string type)
    {
        // Act
        var exceptionCode = code.AsExceptionCode("Linux");

        // Assert
        Assert.That(exceptionCode, Is.Not.Null);
        Assert.That(exceptionCode!.Type, Is.EqualTo(type));
        Assert.That(exceptionCode.Value, Is.Null.Or.Not.Empty);
    }
    
    [Test]
    [TestCase(1u, "EXC_BAD_ACCESS")]
    [TestCase(2u, "EXC_BAD_INSTRUCTION")]
    [TestCase(3u, "EXC_ARITHMETIC")]
    public void AsExceptionCode_MacOS(uint code, string type)
    {
        // Act
        var exceptionCode = code.AsExceptionCode("macOS");

        // Assert
        Assert.That(exceptionCode, Is.Not.Null);
        Assert.That(exceptionCode!.Type, Is.EqualTo(type));
        Assert.That(exceptionCode.Value, Is.Null.Or.Not.Empty);
    }

    [Test]
    public void AsExceptionCode_UnknownCode()
    {
        // Act
        var exceptionCode = 0xfefefefe.AsExceptionCode("Linux");

        // Assert
        Assert.That(exceptionCode, Is.Null);
    }

    [Test]
    public void AsExceptionCode_UnknownOS()
    {
        // Act
        var exceptionCode = 1u.AsExceptionCode("Unknown");

        // Assert
        Assert.That(exceptionCode, Is.Null);
    }
}
