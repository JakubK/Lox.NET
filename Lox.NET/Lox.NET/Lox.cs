namespace Lox.NET;

public static class Lox
{
    private static bool _hadError = false;

    public static async Task RunFileAsync(string path)
    {
        var text = await File.ReadAllTextAsync(path);
        Run(text);

        if (_hadError) Environment.Exit(65);
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
        foreach (var token in tokens)
        {
            Console.WriteLine(token);
        }
    }

    public static void Error(int line, string message)
    {
        Report(line, "", message);
    }

    public static void Report(int line, string where, string message)
    {
        Console.Error.WriteLine($"[line {line} Error {where}: {message}");
    }

}