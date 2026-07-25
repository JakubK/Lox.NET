using static Lox.NET.Lox;

switch (args.Length)
{
    case > 1:
        Console.WriteLine("Usage: lox [script]");
        Environment.Exit(64);
        break;
    case 1:
        await RunFileAsync(args[0]);
        break;
    default:
        await RunPromptAsync();
        break;
}