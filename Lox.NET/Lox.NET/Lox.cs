using Lox.NET.Exceptions;

namespace Lox.NET;

public static class Lox
{
    private static readonly Interpreter interpreter = new ();
    
    private static bool _hadError;
    private static bool _hadRuntimeError;

    public static async Task RunFileAsync(string path)
    {
        var text = await File.ReadAllTextAsync(path);
        Run(text);

        if (_hadError) Environment.Exit(65);
        if (_hadRuntimeError) Environment.Exit(70);
    }

    public static async Task RunPromptAsync()
    {
        await using var input = Console.OpenStandardInput();
        using var sr = new StreamReader(input);
    
        for (;;)
        {
            Console.Write("> ");
            var line = await sr.ReadLineAsync();
            if (line == null) break;
            Run(line);
            _hadError = false;
        }
    }

    public static void Run(string sourceCode)
    {
        var scanner = new Scanner(sourceCode);
        var tokens = scanner.ScanTokens();

        var parser = new Parser(tokens);
        var statements = parser.Parse();
        
        if (_hadError) return;

        interpreter.Interpret(statements);
        
        //Console.WriteLine(new AstPrinter().Print(expression));
    }

    public static void Error(int line, string message)
    {
        Report(line, "", message);
    }

    public static void Report(int line, string where, string message)
    {
        Console.Error.WriteLine($"[line {line} Error {where}: {message}");
    }

    public static void RuntimeError(LoxRuntimeException exception)
    {
        Console.WriteLine($"{exception.Message} \n[line {exception.Token.Line}]");
        _hadRuntimeError = true;
    }
}