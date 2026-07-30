using Lox.Generator;

if (args.Length != 1)
{
    Console.WriteLine("Usage: generate_ast <output_directory>");
    Environment.Exit(64);
}

await Generator.DefineAstAsync(args[0], "Lox.NET.Expression","IExpression", [
    "Ternary : IExpression condition, Token firstOperatorToken, IExpression ifTrue, Token secondOperatorToken, IExpression ifFalse",
    "Assign : Token name, IExpression right",
    "Binary : IExpression left, Token op, IExpression right",
    "Grouping : IExpression expression",
    "Literal : Object val",
    "Logical : IExpression left, Token op, IExpression right",
    "Unary : Token op, IExpression right",
    "Variable : Token name"
], []);

await Generator.DefineAstAsync(args[0], "Lox.NET.Statement","IStatement", [
    "Statement : IExpression expr",
    "Block : List<IStatement> statements",
    "If : IExpression condition, IStatement thenBranch, IStatement elseBranch",
    "Print : IExpression expr",
    "Var : Token name, IExpression initializer",
    "While : IExpression condition, IStatement body",
    "Break : ",
    "Continue : "
], ["Lox.NET.Expression"]);
