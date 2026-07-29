using System.Diagnostics;
using Lox.NET.Exceptions;
using Lox.NET.Expression;

namespace Lox.NET;

public class Interpreter : IVisitor<object>
{
    public void Interpret(IExpression expression)
    {
        try
        {
            var val = Evaluate(expression);
            Console.WriteLine(Stringify(val));
        }
        catch (LoxRuntimeException ex)
        {
            Lox.RuntimeError(ex);
        }
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
        if (left is double && right is double)
        {
            return (double)left + (double)right;
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
}