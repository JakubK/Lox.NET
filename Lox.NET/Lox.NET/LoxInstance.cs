using Lox.NET.Exceptions;

namespace Lox.NET;

public class LoxInstance(LoxClass loxClass)
{
    private readonly Dictionary<string, object> _fields = new();

    public object Get(Token name)
    {
        if (_fields.TryGetValue(name.Lexeme, out var field))
        {
            return field;
        }

        var method = loxClass.FindMethod(name.Lexeme);
        if (method != null)
            return method;

        throw new LoxRuntimeException(name, "Undefined property " + name.Lexeme);
    }

    public void Set(Token name, object val)
    {
        _fields[name.Lexeme] = val;
    }
    
    public override string ToString()
    {
        return loxClass.Name + " instance";
    }
}