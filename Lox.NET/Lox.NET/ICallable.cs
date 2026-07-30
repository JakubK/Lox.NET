namespace Lox.NET;

public interface ICallable
{
    int Arity();
    object? Call(Interpreter interpreter, List<object> arguments);
}