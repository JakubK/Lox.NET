using Lox.NET.Exceptions;

namespace Lox.NET;

public class VariableEnvironment(VariableEnvironment? enclosing)
{
    private readonly Dictionary<string, object?> _values = new();

    public VariableEnvironment() : this(null)
    {
    }
    
    public void Define(string name, object? val)
    {
        _values[name] = val;
    }

    public object? Get(Token name)
    {
        if (_values.TryGetValue(name.Lexeme, out var value))
            return value;
        
        if (enclosing != null)
            return enclosing.Get(name);

        throw new LoxRuntimeException(name, $"Undefined variable '{name.Lexeme}'.");
    }

    public void Assign(Token name, object val)
    {
        if (_values.ContainsKey(name.Lexeme))
        {
            _values[name.Lexeme] = val;
            return;
        }

        if (enclosing != null)
        {
            enclosing.Assign(name, val);
            return;
        }
        
        throw new LoxRuntimeException(name, $"Undefined variable '{name.Lexeme}'.");
    }
}