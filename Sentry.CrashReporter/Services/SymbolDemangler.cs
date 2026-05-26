namespace Sentry.CrashReporter.Services;

public interface ISymbolDemangler
{
    string Demangle(string symbol);
}

public class NullDemangler : ISymbolDemangler
{
    public string Demangle(string symbol) => symbol;
}

#if __DESKTOP__
public class SymbolicDemangler : ISymbolDemangler
{
    public string Demangle(string symbol)
    {
        if (string.IsNullOrEmpty(symbol)) return symbol;

        try
        {
            return global::Sentry.Symbolic.Demangle(symbol) ?? symbol;
        }
        catch (Exception)
        {
            return symbol;
        }
    }
}
#endif
