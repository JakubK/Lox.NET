using Lox.NET.Exceptions;

namespace Lox.NET;

public class VariableEnvironment(VariableEnvironment? enclosing)
{
    public VariableEnvironment? Enclosing = enclosing;
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

    private VariableEnvironment Ancestor(int distance)
    {
        var result = this;
        for (int i = 0; i < distance; i++)
        {
            result = result.Enclosing;
        }

        return result;
    }

    public object? GetAt(int distance, string name)
    {
        return Ancestor(distance)._values[name];
    }

    public void AssignAt(int distance, Token name, object val)
    {
        Ancestor(distance)._values[name.Lexeme] = val;
    }
}