namespace Lox.NET.Exceptions;

public class LoxRuntimeException(Token? token, String message) : Exception(message)
{
    public readonly Token? Token = token;
}