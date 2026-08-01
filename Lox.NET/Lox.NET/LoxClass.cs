namespace Lox.NET;

public class LoxClass(string name, Dictionary<string, LoxFunction> methods) : ICallable
{
    public string Name => name;
    
    public override string ToString()
    {
        return name;
    }

    public int Arity()
    {
        var initializer = FindMethod("init");
        if (initializer == null) return 0;

        return initializer.Arity();
    }

    public object? Call(Interpreter interpreter, List<object> arguments)
    {
        var instance = new LoxInstance(this);
        var initializer = FindMethod("init");
        
        initializer?.Bind(instance).Call(interpreter, arguments);
        
        return instance;
    }

    public LoxFunction? FindMethod(string nameLexeme)
    {
        return methods.GetValueOrDefault(nameLexeme);
    }
}