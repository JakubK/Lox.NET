using System.Diagnostics;
using Lox.NET.Exceptions;
using Lox.NET.Expression;
using Lox.NET.Statement;

namespace Lox.NET;

public class Interpreter : IExpressionVisitor<object?>, IStatementVisitor<object>
{
    private VariableEnvironment _environment = new();
    
    
    public void Interpret(IEnumerable<IStatement> statements)
    {
        try
        {
            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }
        catch (LoxRuntimeException ex)
        {
            Lox.RuntimeError(ex);
        }
    }

    private void Execute(IStatement statement)
    {
        statement.Accept(this);
    }

    private string? Stringify(object? val)
    {
        if (val is null) return "nil";

        if (val is double)
        {
            var text = val.ToString();
            if (text!.EndsWith(".0"))
            {
                text = text.Substring(0, text.Length - 2);
            }

            return text;
        }

        return val.ToString();
    }

    public object? VisitAssign(Assign expression)
    {
        var val = Evaluate(expression.Right);
        _environment.Assign(expression.Name, val);
        return val;
    }

    public object VisitBinary(Binary expression)
    {
        var left = Evaluate(expression.Left);
        var right = Evaluate(expression.Right);
        
        return expression.Op.Type switch
        {
            TokenType.Minus => HandleMinusBinary(expression, left, right),
            TokenType.Plus => HandlePlusBinary(expression, left, right),
            TokenType.Slash => HandleSlashBinary(expression, left, right),
            TokenType.Star => HandleStarBinary(expression, left, right),
            TokenType.Greater => HandleGreaterBinary(expression, left, right),
            TokenType.GreaterEqual => HandleGreaterEqualBinary(expression, left, right),
            TokenType.Less => HandleLessBinary(expression, left, right),
            TokenType.LessEqual => HandleLessEqualBinary(expression, left, right),
            TokenType.Bang => IsEqual(left, right),
            TokenType.BangEqual => !IsEqual(left, right),
            
            _ => throw new UnreachableException()
        };
    }

    private object HandleStarBinary(Binary expression, object left, object right)
    {
        CheckNumberOperands(expression.Op, left, right);
        return (double)left * (double)right;
    }

    private object HandleSlashBinary(Binary expression, object left, object right)
    {
        CheckNumberOperands(expression.Op, left, right);
        return (double)left / (double)right;
    }

    private object HandleMinusBinary(Binary expression, object left, object right)
    {
        CheckNumberOperands(expression.Op, left, right);
        return (double)left - (double)right;
    }

    private object HandleLessEqualBinary(Binary expression, object left, object right)
    {
        CheckNumberOperands(expression.Op, left, right);
        return (double)left <= (double)right;
    }

    private object HandleLessBinary(Binary expression, object left, object right)
    {
        CheckNumberOperands(expression.Op, left, right);
        return (double)left < (double)right;
    }

    private object HandleGreaterEqualBinary(Binary expression, object left, object right)
    {
        CheckNumberOperands(expression.Op, left, right);
        return (double)left >= (double)right;
    }

    private object HandleGreaterBinary(Binary expression, object left, object right)
    {
        CheckNumberOperands(expression.Op, left, right);
        return (double)left > (double)right;
    }

    private void CheckNumberOperands(Token op, object left, object right)
    {
        if (left is double && right is double) 
            return;

        throw new LoxRuntimeException(op, "Operands must be numbers");
    }

    public object VisitGrouping(Grouping expression)
    {
        return Evaluate(expression.Expression);
    }

    public object VisitLiteral(Literal expression)
    {
        return expression.Val;
    }

    public object? VisitLogical(Logical expression)
    {
        var left = Evaluate(expression.Left);

        if (expression.Op.Type == TokenType.Or)
        {
            if (IsTruthy(left)) return left;
        }
        else
        {
            if (!IsTruthy(left)) return left;
        }

        return Evaluate(expression.Right);
    }

    public object VisitUnary(Unary expression)
    {
        var right = Evaluate(expression.Right);

        switch (expression.Op.Type)
        {
            case TokenType.Bang:
                return !IsTruthy(right);
            case TokenType.Minus:
                CheckNumberOperand(expression.Op, right);
                return -(double)right;
            default:
                throw new UnreachableException();
        }
    }

    public object? VisitVariable(Variable expression)
    {
        return _environment.Get(expression.Name);
    }

    public object VisitTernary(Ternary expression)
    {
        var condition = Evaluate(expression.Condition);
        var firstOperand = Evaluate(expression.IfTrue);
        var secondOperand = Evaluate(expression.IfFalse);

        if (expression.FirstOperatorToken.Type == TokenType.QuestionMark &&
            expression.SecondOperatorToken.Type == TokenType.Colon)
        {
            if (IsTruthy(condition))
            {
                return firstOperand;
            }

            return secondOperand;
        }

        throw new UnreachableException();
    }
    
    private object Evaluate(IExpression expression)
    {
        return expression.Accept(this);
    }
    
    private bool IsTruthy(object? obj)
    {
        if (obj == null) return false;
        if (obj is bool) return (bool)obj;
        return true;
    }

    private object HandlePlusBinary(Binary binary, object left, object right)
    {
        if (left is double dLeft && right is double dRight)
        {
            return dLeft + dRight;
        }
        
        if (left is string || right is string)
        {
            return left + right.ToString();
        }

        throw new LoxRuntimeException(binary.Op, "Operands must be two numbers or strings");
    }
    
    private void CheckNumberOperand(Token op, object operand)
    {
        if (operand is double) return;
        throw new LoxRuntimeException(op, "Operand must be a number");
    }
    
    private bool IsEqual(object? left, object? right)
    {
        if (left is null && right is null) return true;
        if (left is null) return false;

        return left.Equals(right);
    }

    public object VisitStatement(Statement.Statement statement)
    {
        Evaluate(statement.Expr);
        return null;
    }

    public object VisitBlock(Block expression)
    {
        ExecuteBlock(expression.Statements, new VariableEnvironment(_environment));
        return null;
    }

    public object VisitIf(If statement)
    {
        if (IsTruthy(Evaluate(statement.Condition)))
        {
            Execute(statement.ThenBranch);
        } else if (statement.ElseBranch != null)
        {
            Execute(statement.ElseBranch);
        }

        return null;
    }

    private void ExecuteBlock(List<IStatement> statements, VariableEnvironment variableEnvironment)
    {
        var previous = _environment;
        try
        {
            _environment = variableEnvironment;
            foreach (var statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            _environment = previous;
        }
    }

    public object VisitPrint(Print statement)
    {
        var val = Evaluate(statement.Expr);
        if (val is null)
            throw new LoxRuntimeException(null, "Variable is null");
        Console.WriteLine(Stringify(val));
        return null;
    }

    public object VisitVar(Var expression)
    {
        var value = expression.Initializer != null ? Evaluate(expression.Initializer) : null;
        _environment.Define(expression.Name.Lexeme, value);
        return null;
    }

    public object VisitWhile(While statement)
    {
        while (IsTruthy(Evaluate(statement.Condition)))
        {
            Execute(statement.Body);
        }

        return null;
    }
}