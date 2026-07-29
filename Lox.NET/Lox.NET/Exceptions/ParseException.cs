namespace Lox.NET.Exceptions;

public class ParseException : Exception
{
    public readonly Token? Token;
    
    public ParseException(Token token, string message) : base(message)
    {
        Token = token;
    }
    
    public ParseException()
    {
    }
}