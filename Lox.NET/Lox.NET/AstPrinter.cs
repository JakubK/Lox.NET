using System.Text;
using Lox.NET.Expression;

namespace Lox.NET;

public class AstPrinter : IExpressionVisitor<string>
{
    public string Print(IExpression expression)
    {
        return expression.Accept(this);
    }
    
    public string VisitAssignExpression(Assign expression)
    {
        throw new NotImplementedException();
    }

    public string VisitBinaryExpression(Binary expression)
    {
        return Parenthesize(expression.Op.Lexeme, expression.Left, expression.Right);
    }

    public string VisitGroupingExpression(Grouping expression)
    {
        return Parenthesize("group", expression.Expression);
    }

    public string VisitCallExpression(Call expression)
    {
        throw new NotImplementedException();
    }

    public string VisitGetExpression(Get expression)
    {
        throw new NotImplementedException();
    }

    public string VisitSetExpression(Set expression)
    {
        throw new NotImplementedException();
    }

    public string VisitLiteralExpression(Literal expression)
    {
        if (expression.Val == null) return "nil";
        return expression.Val.ToString()!;
    }

    public string VisitLogicalExpression(Logical expression)
    {
        throw new NotImplementedException();
    }

    public string VisitUnaryExpression(Unary expression)
    {
        return Parenthesize(expression.Op.Lexeme, expression.Right);
    }

    public string VisitVariableExpression(Variable expression)
    {
        throw new NotImplementedException();
    }

    public string VisitTernaryExpression(Ternary expression)
    {
        return Parenthesize(expression.FirstOperatorToken.Lexeme + expression.SecondOperatorToken.Lexeme, expression.Condition,
            expression.IfTrue, expression.IfFalse);
    }

    private string Parenthesize(string name, params IEnumerable<IExpression> expressions)
    {
        var sb = new StringBuilder();
        sb.Append("(").Append(name);
        
        foreach (var expression in expressions)
        {
            sb.Append(" ");
            sb.Append(expression.Accept(this));
        }
        
        sb.Append(")");

        return sb.ToString();
    }
}