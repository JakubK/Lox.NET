using Lox.NET.Expression;
using Lox.NET.Statement;

namespace Lox.NET;

public class Resolver(Interpreter interpreter) : IExpressionVisitor<object>, IStatementVisitor<object>
{
    private readonly Stack<Dictionary<string, bool>> _scopes = new();
    private FunctionType currentFunction = FunctionType.None;
    
    public object VisitTernary(Ternary expression)
    {
        Resolve(expression.Condition);
        Resolve(expression.IfTrue);
        Resolve(expression.IfFalse);
        return null;
    }

    public object VisitAssign(Assign expression)
    {
        Resolve(expression.Right);
        ResolveLocal(expression, expression.Name);
        return null;
    }

    public object VisitBinary(Binary expression)
    {
        Resolve(expression.Left);
        Resolve(expression.Right);
        return null;
    }

    public object VisitGrouping(Grouping expression)
    {
        Resolve(expression.Expression);
        return null;
    }

    public object VisitCall(Call expression)
    {
        Resolve(expression.Callee);

        foreach (var arg in expression.Arguments)
        {
            Resolve(arg);
        }

        return null;
    }

    public object VisitLiteral(Literal expression)
    {
        return null;
    }

    public object VisitLogical(Logical expression)
    {
        Resolve(expression.Left);
        Resolve(expression.Right);
        return null;
    }

    public object VisitUnary(Unary expression)
    {
        Resolve(expression.Right);
        return null;
    }

    public object VisitVariable(Variable expression)
    {
        if (_scopes.Count != 0)
        {
            if (_scopes.Peek().TryGetValue(expression.Name.Lexeme, out var isReady))
            {
                if (!isReady)
                {
                    Lox.Error(expression.Name.Line, "Can't read local variable in its own initializer");
                }
            }
        }

        ResolveLocal(expression, expression.Name);
        return null;
    }

    public object VisitStatement(Statement.Statement statement)
    {
        Resolve(statement.Expr);
        return null;
    }

    public object VisitBlock(Block statement)
    {
        BeginScope();
        Resolve(statement.Statements);
        EndScope();
        return null;
    }

    public object VisitFunction(Function statement)
    {
        Declare(statement.Name);
        Define(statement.Name);

        ResolveFunction(statement, FunctionType.Function);
        return null;
    }

    public object VisitIf(If statement)
    {
        Resolve(statement.Condition);
        Resolve(statement.ThenBranch);
        if (statement.ElseBranch != null)
        {
            Resolve(statement.ElseBranch);
        }

        return null;
    }

    public object VisitPrint(Print statement)
    {
        if (statement.Expr != null)
        {
            Resolve(statement.Expr);
        }

        return null;
    }

    public object VisitVar(Var statement)
    {
        Declare(statement.Name);
        if (statement.Initializer != null)
        {
            Resolve(statement.Initializer);
        }

        Define(statement.Name);
        return null;
    }

    public object VisitWhile(While statement)
    {
        Resolve(statement.Condition);
        Resolve(statement.Body);
        return null;
    }

    public object VisitReturn(Return statement)
    {
        if (currentFunction == FunctionType.None)
        {
            Lox.Error(statement.Keyword.Line, "Cant return from top-level code");
        }

        if (statement.Value != null)
        {
            Resolve(statement.Value);
        }

        return null;
    }

    public object VisitBreak(Break expression)
    {
        return null;
    }

    public object VisitContinue(Continue expression)
    {
        return null;
    }
    
    public void Resolve(List<IStatement> statements)
    {
        foreach (var statement in statements)
        {
            Resolve(statement);
        }
    }

    public void Resolve(IStatement statement)
    {
        statement.Accept(this);
    }

    private void Resolve(IExpression expression)
    {
        expression.Accept(this);
    }
    
    private void BeginScope()
    {
        _scopes.Push(new ());
    }
    
    private void EndScope()
    {
        _scopes.Pop();
    }
    
    private void Declare(Token name)
    {
        if (_scopes.Count == 0) return;

        if (_scopes.Peek().ContainsKey(name.Lexeme))
        {
            Lox.Error(name.Line, "There already is a variable with this name in this scope");
        }

        _scopes.Peek()[name.Lexeme] = false;
    }
    
    private void Define(Token name)
    {
        if (_scopes.Count == 0) return;
        
        _scopes.Peek()[name.Lexeme] = true;
    }
    
    private void ResolveLocal(IExpression expression, Token name)
    {
        for (int i = _scopes.Count - 1; i >= 0; i--)
        {
            if (_scopes.ElementAt(i).ContainsKey(name.Lexeme))
            {
                interpreter.Resolve(expression, _scopes.Count - 1 - i);
                return;
            }
        }
    }
    
    private void ResolveFunction(Function statement, FunctionType type)
    {
        var enclosingFunction = currentFunction;
        currentFunction = type;
        
        BeginScope();
        foreach (var param in statement.Parameters)
        {
            Declare(param);
            Define(param);
        }
        Resolve(statement.Body);
        EndScope();
        currentFunction = enclosingFunction;
    }
}