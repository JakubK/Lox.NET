using System.Globalization;

namespace Lox.NET;

public class Scanner(string source)
{
    private readonly Dictionary<string, TokenType> _keywords = new()
    {
        { "and", TokenType.And },
        { "class", TokenType.Class },
        { "else", TokenType.Else },
        { "false", TokenType.False },
        { "for", TokenType.For },
        { "fun", TokenType.Fun },
        { "if", TokenType.If },
        { "nil", TokenType.Nil },
        { "or", TokenType.Or },
        { "print", TokenType.Print },
        { "return", TokenType.Return },
        { "super", TokenType.Super },
        { "this", TokenType.This },
        { "true", TokenType.True },
        { "var", TokenType.Var },
        { "while", TokenType.While }
    };
    private readonly List<Token> _tokens = new();
    private int _start;
    private int _current;
    private int _line = 1;

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd)
        {
            _start = _current;
            ScanToken();
        }
        
        _tokens.Add(new Token(TokenType.Eof, "", null, _line));
        return _tokens;
    }

    private void ScanToken()
    {
        var c = Advance();
        switch (c)
        {
            case '(' : AddToken(TokenType.LeftParen); break;
            case ')' : AddToken(TokenType.RightParen); break;
            case '{' : AddToken(TokenType.LeftBrace); break;
            case '}' : AddToken(TokenType.RightBrace); break;
            case ',' : AddToken(TokenType.Comma); break;
            case '.' : AddToken(TokenType.Dot); break;
            case '-' : AddToken(TokenType.Minus); break;
            case '+' : AddToken(TokenType.Plus); break;
            case ';' : AddToken(TokenType.Semicolon); break;
            case '*' : AddToken(TokenType.Star); break;
            case ':' : AddToken(TokenType.Colon); break;
            case '?' : AddToken(TokenType.QuestionMark); break;
            case '!' : AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang); break;
            case '=' : AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal); break;
            case '<' : AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less); break;
            case '>' : AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater); break;
            case '/':
                if (Match('/'))
                {
                    // A comment goes until the end of the line
                    while (Peek() != '\n' && !IsAtEnd)
                    {
                        Advance();
                    }
                }
                else if (Match('*'))
                {
                    // A comment goes until the end of the file or matching */
                    while (!(Peek() == '*' && PeekNext() == '/') && !IsAtEnd)
                    {
                        Advance();
                    }

                    if (!IsAtEnd)
                    {
                        // Jump through the */
                        Advance();
                        Advance();
                    }
                }
                else
                {
                    AddToken(TokenType.Slash);
                }
                break;
            case ' ':
            case '\r':
            case '\t':
                break;
            case '\n':
                _line++;
                break;
            case '"': String(); break;
            default:
                if (IsDigit(c))
                {
                    Number();
                }
                else if (IsAlpha(c))
                {
                    Identifier();
                }
                else
                {
                    Lox.Error(_line, "Unexpected character");
                }
                break;
        }
    }

    private void Identifier()
    {
        while (IsAlphaNumeric(Peek())) Advance(); // Consume everything alphanumeric as long as possible

        var text = Utils.Substring(source, _start, _current);
        var type = _keywords.GetValueOrDefault(text, TokenType.Identifier); // Try to match the keyword, fallback to Identifier (variableName, fieldName etc.)
        
        AddToken(type);
    }

    private bool IsAlphaNumeric(char c)
    {
        return IsAlpha(c) || IsDigit(c);
    }

    private bool IsAlpha(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
    }

    private void Number()
    {
        while (IsDigit(Peek())) Advance(); // Consume integral part
        
        // look for fractional part
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance(); // Consume '.'
            while (IsDigit(Peek())) Advance(); // Consume fractional part
        }
        
        AddToken(TokenType.Number, double.Parse(Utils.Substring(source, _start, _current), CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Get character that would be pointed if pointer got incremented
    /// </summary>
    /// <returns></returns>
    private char PeekNext()
    {
        if (_current + 1 >= source.Length) return '\0';
        return source[_current + 1];
    }

    private bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private void String()
    {
        while (Peek() != '"' && !IsAtEnd)
        {
            if (Peek() == '\n') _line++; // Support multiline string
            Advance(); // Consume as string everything before final " token
        }

        if (IsAtEnd)
        {
            Lox.Error(_line, "Unterminated string");
            return;
        }

        Advance(); // Closing "
        
        var value = Utils.Substring(source, _start + 1, _current - 1);
        AddToken(TokenType.String, value);
    }

    /// <summary>
    /// Get character currently pointed without touching pointer
    /// </summary>
    /// <returns></returns>
    private char Peek()
    {
        if (IsAtEnd) return '\0';
        return source[_current];
    }

    /// <summary>
    /// Increment the pointer if it points expected character
    /// </summary>
    /// <param name="expected"></param>
    /// <returns></returns>
    private bool Match(char expected)
    {
        if (IsAtEnd) return false;
        if (source[_current] != expected) return false;

        _current++;
        return true;
    }

    /// <summary>
    /// Get character currently pointed by pointer and then increment the pointer
    /// </summary>
    /// <returns></returns>
    private char Advance()
    {
        return source[_current++];
    }

    private void AddToken(TokenType type, object? literal = null)
    {
        var text = Utils.Substring(source, _start, _current);
        _tokens.Add(new (type, text, literal, _line));
    }

    private bool IsAtEnd => _current >= source.Length;
}