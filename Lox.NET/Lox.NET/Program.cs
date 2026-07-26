using Lox.NET;
using Lox.NET.Expression;
using static Lox.NET.Lox;

// switch (args.Length)
// {
//     case > 1:
//         Console.WriteLine("Usage: lox [script]");
//         Environment.Exit(64);
//         break;
//     case 1:
//         await RunFileAsync(args[0]);
//         break;
//     default:
//         await RunPromptAsync();
//         break;
// }

var unary = new Unary(
    new Token(TokenType.Minus, "-", null, 1),
    new Literal(123));

var expression = new Binary(
    unary,
    new Token(TokenType.Star, "*", null, 1),
    new Grouping(new Literal(45.67)));
    
    Console.WriteLine(new AstPrinter().Print(expression));