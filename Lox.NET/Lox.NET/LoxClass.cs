namespace Lox.NET;

public class LoxClass(string name, Dictionary<string, LoxFunction> methods) : ICallable
{
    public string Name => name;
    
    public override string ToString()
    {
        return name;
    }

    public int Arity() => 0;

    public object? Call(Interpreter interpreter, List<object> arguments)
    {
        var instance = new LoxInstance(this);
        return instance;
    }

    public object? FindMethod(string nameLexeme)
    {
        return methods.GetValueOrDefault(nameLexeme);
    }
}