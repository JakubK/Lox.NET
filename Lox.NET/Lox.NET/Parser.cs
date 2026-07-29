using Lox.NET.Exceptions;
using Lox.NET.Expression;

namespace Lox.NET;

public class Parser(List<Token> tokens)
{
    private int _current;
    private bool IsAtEnd => Peek.Type == TokenType.Eof;

    private Token Peek => tokens[_current];
    private Token Previous => tokens[_current - 1];

    public IExpression? Parse()
    {
        try
        {
            return Expression();
        }
        catch (ParseException error)
        {
            return null;
        }
    }

    private IExpression Expression()
    {
        return Comma();
    }
    
    private IExpression Comma()
    {
        // Conditional ("," Conditional)*
        var expr = Conditional();
        
        while (Match(TokenType.Comma))
        {
            var op = Previous;
            var right = Conditional();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    private IExpression Conditional()
    {
        //  equality ("?" expression ":" conditional)?
        var expr = Equality();
        if (Match(TokenType.QuestionMark))
        {
            var questionMark = Previous;
            var ifTrue = Expression();
            if (Match(TokenType.Colon))
            {
                var colon = Previous;
                var ifFalse = Conditional();
                expr = new Ternary(expr, questionMark, ifTrue, colon, ifFalse);
            }
        }

        return expr;
    }

    private IExpression Equality()
    {
        // comparison ( ( "!=" | "==" ) comparison )*
        
        var expr = Comparison();
        while (Match(TokenType.BangEqual, TokenType.EqualEqual))
        {
            var op = Previous;
            var right = Comparison();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    private IExpression Comparison()
    {
        var expr = Term();

        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            var op = Previous;
            var right = Term();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    private IExpression Term()
    {
        var expr = Factor();

        while (Match(TokenType.Minus, TokenType.Plus))
        {
            var op = Previous;
            var right = Factor();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    private IExpression Factor()
    {
        var expr = Unary();
        while (Match(TokenType.Slash, TokenType.Star))
        {
            var op = Previous;
            var right = Unary();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    private IExpression Unary()
    {
        if (Match(TokenType.Bang, TokenType.Minus))
        {
            var op = Previous;
            var right = Unary();
            return new Unary(op, right);
        }

        return Primary();
    }

    private IExpression Primary()
    {
        if (Match(TokenType.False)) return new Literal(false);
        if (Match(TokenType.True)) return new Literal(true);
        if (Match(TokenType.Nil)) return new Literal(null);

        if (Match(TokenType.Number, TokenType.String))
        {
            return new Literal(Previous.Literal);
        }

        if (Match(TokenType.LeftParen))
        {
            var expr = Expression(); // expression between ( and ). Start parser from the beginning
            Consume(TokenType.RightParen, "Expect ')' after expression.");
            return new Grouping(expr);
        }
        
        Error(Peek, "Expected expression");
        throw new ParseException();
    }

    /// <summary>
    /// Skip tokens to skip the entire block
    /// </summary>
    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd)
        {
            if (Previous.Type == TokenType.Semicolon) return;

            switch (Peek.Type)
            {
                case TokenType.Class:
                case TokenType.Fun:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Print:
                case TokenType.Return:
                    return;
            }

            Advance();
        }
    }

    /// <summary>
    /// Move the pointer if currently selected token is of given type
    /// Throw error otherwise
    /// </summary>
    /// <param name="type"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="ParseException"></exception>
    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();

        Error(Peek, message);
        throw new ParseException();
    }

    private void Error(Token token, string message)
    {
        if (token.Type == TokenType.Eof)
        {
            Lox.Report(token.Line, " at end", message);
        }
        else
        {
            Lox.Report(token.Line, " at '" + token.Lexeme + "'", message);
        }

        throw new ParseException();
    }

    /// <summary>
    /// Return true after first matched token
    /// And move the pointer as side effect
    /// </summary>
    /// <param name="types"></param>
    /// <returns></returns>
    private bool Match(params IEnumerable<TokenType> types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if currently pointed Token is of given type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private bool Check(TokenType type)
    {
        if (IsAtEnd) return false;
        return Peek.Type == type;
    }

    /// <summary>
    /// Increment the pointer and return Token which was previously selected
    /// </summary>
    /// <returns></returns>
    private Token Advance()
    {
        if (!IsAtEnd) _current++;
        return Previous;
    }
}