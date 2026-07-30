using Lox.NET.Exceptions;
using Lox.NET.Statement;

namespace Lox.NET;

public class LoxFunction(Function declaration) : ICallable
{
    public int Arity() => declaration.Parameters.Count;

    public object? Call(Interpreter interpreter, List<object> arguments)
    {
        var env = new VariableEnvironment(interpreter.Globals);

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
            return ex.Value;
        }
        
        return null;
    }

    public override string ToString()
    {
        return $"<fn {declaration.Name.Lexeme}>";
    }
}