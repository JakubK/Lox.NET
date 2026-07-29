using Lox.Generator;

if (args.Length != 1)
{
    Console.WriteLine("Usage: generate_ast <output_directory>");
    Environment.Exit(64);
}

await Generator.DefineAstAsync(args[0], "Lox.NET.Expression","IExpression", [
    "Binary : IExpression left, Token op, IExpression right",
    "Grouping : IExpression expression",
    "Literal : Object val",
    "Unary : Token op, IExpression right",
    "Ternary : IExpression condition, Token firstOperatorToken, IExpression ifTrue, Token secondOperatorToken, IExpression ifFalse",
]);


