using Lox.NET.Exceptions;

namespace Lox.NET;

public class VariableEnvironment
{
    private readonly Dictionary<string, object?> _values = new();

    public void Define(string name, object? val)
    {
        _values[name] = val;
    }

    public object? Get(Token name)
    {
        if (_values.TryGetValue(name.Lexeme, out var value))
        {
            return value;
        }

        throw new LoxRuntimeException(name, $"Undefined variable '{name.Lexeme}'.");
    }
}