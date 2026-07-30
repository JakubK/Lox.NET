namespace Lox.NET.Exceptions;

public class ReturnException(object? val) : Exception
{
    public object? Value => val;
}