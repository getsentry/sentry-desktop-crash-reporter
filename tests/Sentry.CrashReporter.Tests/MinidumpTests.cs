namespace Sentry.CrashReporter.Tests;

public class MinidumpTests
{
    [Test]
    public void Exception()
    {
        // Act
        var minidump = Minidump.FromFile("data/minidump.dmp");
        var exception = minidump.Streams.Select(s => s.Data).OfType<Minidump.ExceptionStream>().SingleOrDefault();

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ExceptionRec.Code, Is.EqualTo(0xc0000005));
    }

    [Test]
    public void SystemInfo()
    {
        // Act
        var minidump = Minidump.FromFile("data/minidump.dmp");
        var systemInfo = minidump.Streams.Select(s => s.Data).OfType<Minidump.SystemInfo>().FirstOrDefault();

        // Assert
        Assert.That(systemInfo, Is.Not.Null);
        Assert.That(systemInfo!.CpuArch, Is.EqualTo(Minidump.SystemInfo.CpuArchs.Amd64));
        Assert.That(systemInfo.CpuLevel, Is.EqualTo(25));
        Assert.That(systemInfo.CpuRevision, Is.EqualTo(8448));
        Assert.That(systemInfo.NumCpus, Is.EqualTo(12));
        Assert.That(systemInfo.OsType, Is.EqualTo(1));
        Assert.That(systemInfo.OsVerMajor, Is.EqualTo(10));
        Assert.That(systemInfo.OsVerMinor, Is.EqualTo(0));
        Assert.That(systemInfo.OsBuild, Is.EqualTo(22621));
        Assert.That(systemInfo.OsPlatform, Is.EqualTo(2));
    }

    [Test]
    public void MiscInfo()
    {
        // Act
        var minidump = Minidump.FromFile("data/minidump.dmp");
        var miscInfo = minidump.Streams.Select(s => s.Data).OfType<Minidump.MiscInfo>().FirstOrDefault();

        // Assert
        Assert.That(miscInfo, Is.Not.Null);
        Assert.That(miscInfo!.ProcessId, Is.EqualTo(33180));
        Assert.That(miscInfo.ProcessCreateTime, Is.EqualTo(1729543080));
        Assert.That(miscInfo.ProcessUserTime, Is.Zero);
        Assert.That(miscInfo.ProcessKernelTime, Is.Zero);
        Assert.That(miscInfo.CpuMaxMhz, Is.EqualTo(3701));
        Assert.That(miscInfo.CpuCurMhz, Is.EqualTo(3701));
        Assert.That(miscInfo.CpuLimitMhz, Is.EqualTo(3701));
        Assert.That(miscInfo.CpuMaxIdleState, Is.EqualTo(2));
        Assert.That(miscInfo.CpuCurIdleState, Is.EqualTo(1));
    }

    [Test]
    public void ThreadList()
    {
        // Act
        var minidump = Minidump.FromFile("data/minidump.dmp");
        var threadList = minidump.Streams.Select(s => s.Data).OfType<Minidump.ThreadList>().FirstOrDefault();
        var thread = threadList?.Threads.FirstOrDefault();

        // Assert
        Assert.That(threadList, Is.Not.Null);
        Assert.That(threadList!.Threads, Is.Not.Empty);
        Assert.That(thread, Is.Not.Null);
        Assert.That(thread!.ThreadId, Is.Not.Zero);
    }
}
