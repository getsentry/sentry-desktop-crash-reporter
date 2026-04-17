using System.Runtime.InteropServices;

namespace Sentry.CrashReporter;

internal static class CommandLineArgs
{
    internal static string[] Get()
    {
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        // Uno's macOS .app launcher invokes coreclr_execute_assembly with argc=0/argv=NULL,
        // so the managed runtime's view of argv is empty. Fall back to reading the real
        // process argv directly via libSystem.
        if (args.Length == 0 && OperatingSystem.IsMacOS())
        {
            args = NSGetArgs();
        }
        return args;
    }

    private static string[] NSGetArgs()
    {
        var argc = Marshal.ReadInt32(_NSGetArgc());
        var argv = Marshal.ReadIntPtr(_NSGetArgv());
        var result = new string[Math.Max(0, argc - 1)];
        for (var i = 1; i < argc; i++)
        {
            var argPtr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
            result[i - 1] = Marshal.PtrToStringUTF8(argPtr) ?? "";
        }
        return result;
    }

    [DllImport("libSystem.dylib")]
    private static extern IntPtr _NSGetArgc();

    [DllImport("libSystem.dylib")]
    private static extern IntPtr _NSGetArgv();
}
