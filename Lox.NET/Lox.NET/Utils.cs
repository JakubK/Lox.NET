namespace Lox.NET;

public static class Utils
{
    public static string Substring(string input, int startIndex, int endIndex)
    {
        var length = endIndex - startIndex;
        return input.Substring(startIndex, length);
    }
}