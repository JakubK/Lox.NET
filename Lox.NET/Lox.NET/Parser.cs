using Lox.NET.Exceptions;
using Lox.NET.Expression;
using Lox.NET.Statement;

namespace Lox.NET;

public class Parser(List<Token> tokens)
{
    private int _current;
    private bool IsAtEnd => Peek.Type == TokenType.Eof;

    private Token Peek => tokens[_current];
    private Token Previous => tokens[_current - 1];

    public List<IStatement> Parse()
    {
        var result = new List<IStatement>();

        while (!IsAtEnd)
        {
            result.Add(Declaration());
        }

        return result;
    }

    public int Reset()
    {
        var cache = _current;
        _current = 0;
        
        return cache;
    }
    
    public IExpression? ParseExpression()
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

    private IStatement Declaration()
    {
        // declaration -> VariableDeclaration | Statement
        try
        {
            if (Match(TokenType.Var))
                return VariableDeclaration();
            return Statement();
        }
        catch (ParseException ex)
        {
            Synchronize();
            return null;
        }
    }

    private IStatement VariableDeclaration()
    {
        // VariableDeclaration -> "var" IDENTIFIER ("=" expression)? ";"
        var name = Consume(TokenType.Identifier, "Expect variable name");
        var initializer = Match(TokenType.Equal) ? Expression() : null;
        Consume(TokenType.Semicolon, "Expect ';' after variable declaration");
        
        return new Var(name, initializer);
    }

    private IStatement Statement()
    {
        // statement -> ifStatement | printStatement | expressionStatement | block
        if (Match(TokenType.If))
            return IfStatement();
        if (Match(TokenType.Print))
            return PrintStatement();
        if (Match(TokenType.LeftBrace))
            return new Block(Block());

        return ExpressionStatement();
    }

    private IStatement IfStatement()
    {
        Consume(TokenType.LeftParen, "Expect '(' after 'if'");
        var condition = Expression();
        Consume(TokenType.RightParen, "Expect ')' after 'if'");

        var thenBranch = Statement();
        var elseBranch = Match(TokenType.Else) ? Statement() : null;

        return new If(condition, thenBranch, elseBranch);
    }

    private List<IStatement> Block()
    {
        var statements = new List<IStatement>();

        while (!Check(TokenType.RightBrace) && !IsAtEnd)
        {
            statements.Add(Declaration());
        }

        Consume(TokenType.RightBrace, "Expect '}' after block.");
        return statements;
    }

    private IStatement ExpressionStatement()
    {
        // expressionStatement -> expression
        var expr = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after expression");
        return new Statement.Statement(expr);
    }

    private IStatement PrintStatement()
    {
        // printStatement -> expression
        var val = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after value");
        return new Print(val);
    }

    private IExpression Expression()
    {
        return Assignment();
    }

    private IExpression Assignment()
    {
        // assignment -> or ('=' assignment)?
        
        var expr = Or();
        if (Match(TokenType.Equal))
        {
            var equals = Previous;
            var val = Assignment();

            if (expr is Variable)
            {
                var name = ((Variable)expr).Name;
                return new Assign(name, val);
            }
            
            Error(equals, "Invalid assignment target");
        }

        return expr;
    }

    private IExpression Or()
    {
        var expr = And();
        while (Match(TokenType.Or))
        {
            var op = Previous;
            var right = And();
            expr = new Logical(expr, op, right);
        }

        return expr;
    }

    private IExpression And()
    {
        var expr = Comma();

        while (Match(TokenType.And))
        {
            var op = Previous;
            var right = Equality();
            expr = new Logical(expr, op, right);
        }

        return expr;
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
        // term ( ( ">" | ">=" | "<" | "<=" ) term )*
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
        // factor ( ( "-" | "+" ) factor )*
        
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
        // unary ( ( "/" | "*" ) unary )*
        
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
        // ( ( "!" | "-" ) unary ) | primary
        
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
        // NUMBER | STRING | LITERAL | "true" | "false" | "nil" | "(" expression ")"
        
        if (Match(TokenType.Number, TokenType.String))
        {
            return new Literal(Previous.Literal);
        }

        if (Match(TokenType.Identifier))
        {
            return new Variable(Previous);
        }
        
        if (Match(TokenType.False)) return new Literal(false);
        if (Match(TokenType.True)) return new Literal(true);
        if (Match(TokenType.Nil)) return new Literal(null);
        
        if (Match(TokenType.LeftParen))
        {
            var expr = Expression(); // expression between ( and ). Start parser from the beginning
            Consume(TokenType.RightParen, "Expect ')' after expression.");
            return new Grouping(expr);
        }
        
        throw new ParseException(Peek, "Expected expression");
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