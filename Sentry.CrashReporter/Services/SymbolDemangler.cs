namespace Sentry.CrashReporter.Services;

public interface ISymbolDemangler
{
    string Demangle(string symbol);
}

public class SymbolDemangler : ISymbolDemangler
{
#if __DESKTOP__
    private bool _enabled = true;
#endif

    public string Demangle(string symbol)
    {
        if (string.IsNullOrEmpty(symbol)) return symbol;

#if __DESKTOP__
        if (!_enabled) return symbol;

        try
        {
            return global::Sentry.Symbolic.Demangle(symbol) ?? symbol;
        }
        catch (DllNotFoundException)
        {
            _enabled = false;
            return symbol;
        }
        catch (EntryPointNotFoundException)
        {
            _enabled = false;
            return symbol;
        }
        catch (BadImageFormatException)
        {
            _enabled = false;
            return symbol;
        }
        catch (PlatformNotSupportedException)
        {
            _enabled = false;
            return symbol;
        }
        catch (Exception)
        {
            return symbol;
        }
#else
        return symbol;
#endif
    }
}
