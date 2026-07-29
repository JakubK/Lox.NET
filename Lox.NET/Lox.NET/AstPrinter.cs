using System.Text;
using Lox.NET.Expression;

namespace Lox.NET;

public class AstPrinter : IVisitor<string>
{
    public string Print(IExpression expression)
    {
        return expression.Accept(this);
    }
    
    public string VisitBinary(Binary expression)
    {
        return Parenthesize(expression.Op.Lexeme, expression.Left, expression.Right);
    }

    public string VisitGrouping(Grouping expression)
    {
        return Parenthesize("group", expression.Expression);
    }

    public string VisitLiteral(Literal expression)
    {
        if (expression.Val == null) return "nil";
        return expression.Val.ToString()!;
    }

    public string VisitUnary(Unary expression)
    {
        return Parenthesize(expression.Op.Lexeme, expression.Right);
    }

    public string VisitTernary(Ternary expression)
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