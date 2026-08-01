using Lox.NET.Exceptions;
using Lox.NET.Statement;

namespace Lox.NET;

public class LoxFunction(Function declaration, VariableEnvironment closure, bool isInitializer) : ICallable
{
    public int Arity() => declaration.Parameters.Count;

    public object? Call(Interpreter interpreter, List<object> arguments)
    {
        var env = new VariableEnvironment(closure);

        for (int i = 0; i < declaration.Parameters.Count; i++)
        {
            env.Define(declaration.Parameters[i].Lexeme, arguments[i]);
        }

        try
        {
            interpreter.ExecuteBlock(declaration.Body, env);
        }
        catch (ReturnException ex)
        {
            if (isInitializer) return closure.GetAt(0, "this");
            
            return ex.Value;
        }

        if (isInitializer) return closure.GetAt(0, "this");
        
        return null;
    }

    public override string ToString()
    {
        return $"<fn {declaration.Name.Lexeme}>";
    }

    public LoxFunction Bind(LoxInstance instance)
    {
        var env = new VariableEnvironment(closure);
        env.Define("this", instance);
        
        return new(declaration, env, isInitializer);
    }
}