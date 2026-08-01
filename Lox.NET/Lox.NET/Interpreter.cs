using System.Diagnostics;
using Lox.NET.Exceptions;
using Lox.NET.Expression;
using Lox.NET.Statement;

namespace Lox.NET;

public class Interpreter : IExpressionVisitor<object?>, IStatementVisitor<object>
{
    public readonly VariableEnvironment Globals = new();
    private VariableEnvironment _environment;
    private readonly Dictionary<IExpression, int?> _locals = new();

    public Interpreter()
    {
        _environment = Globals;
        
        Globals.Define("clock", new Clock());
    }
    
    
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

    public object? VisitAssignExpression(Assign expression)
    {
        var val = Evaluate(expression.Right);

        var distance = _locals.GetValueOrDefault(expression);
        if (distance != null)
        {
            _environment.AssignAt((int)distance, expression.Name, val);
        }
        else
        {
            Globals.Assign(expression.Name, val);
        }
        
        return val;
    }

    public object VisitBinaryExpression(Binary expression)
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
            TokenType.EqualEqual => IsEqual(left, right),
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

    public object VisitGroupingExpression(Grouping expression)
    {
        return Evaluate(expression.Expression);
    }

    public object? VisitCallExpression(Call expression)
    {
        var callee = Evaluate(expression.Callee);
        var arguments = expression.Arguments.Select(Evaluate).ToList();

        if (!(callee is ICallable func))
            throw new LoxRuntimeException(expression.Paren, "Can only call functions and classes");

        if (arguments.Count != func.Arity())
            throw new LoxRuntimeException(expression.Paren, $"Expected {func.Arity()} arguments but got {arguments.Count}");
        
        return func.Call(this, arguments);
    }

    public object? VisitGetExpression(Get expression)
    {
        var obj = Evaluate(expression.Obj);
        if (obj is LoxInstance instance)
        {
            return instance.Get(expression.Name);
        }

        throw new LoxRuntimeException(expression.Name, "Only instances have properties");
    }

    public object? VisitSetExpression(Set expression)
    {
        var obj = Evaluate(expression.Obj);

        if (obj is not LoxInstance instance)
        {
            throw new LoxRuntimeException(expression.Name, "Only instances have fields");
        }

        var val = Evaluate(expression.Val);
        instance.Set(expression.Name, val);
        return val;
    }

    public object VisitLiteralExpression(Literal expression)
    {
        return expression.Val;
    }

    public object? VisitLogicalExpression(Logical expression)
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

    public object VisitUnaryExpression(Unary expression)
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

    public object? VisitVariableExpression(Variable expression)
    {
        return LookupVariable(expression.Name, expression);
    }

    public object VisitTernaryExpression(Ternary expression)
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

    public object VisitStatementStatement(Statement.Statement statement)
    {
        Evaluate(statement.Expr);
        return null;
    }

    public object VisitBlockStatement(Block expression)
    {
        ExecuteBlock(expression.Statements, new VariableEnvironment(_environment));
        return null;
    }

    public object VisitClassStatement(Class statement)
    {
        _environment.Define(statement.Name.Lexeme, null);

        var methods = new Dictionary<string, LoxFunction>();
        foreach (var method in statement.Methods)
        {
            var function = new LoxFunction(method, _environment);
            methods[method.Name.Lexeme] = function;
        }
        
        var loxClass = new LoxClass(statement.Name.Lexeme, methods);
        _environment.Assign(statement.Name, loxClass);
        return null;
    }

    public object VisitFunctionStatement(Function statement)
    {
        var func = new LoxFunction(statement, _environment);
        _environment.Define(statement.Name.Lexeme, func);
        return null;
    }

    public object VisitIfStatement(If statement)
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

    public void ExecuteBlock(List<IStatement> statements, VariableEnvironment variableEnvironment)
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

    public object VisitPrintStatement(Print statement)
    {
        var val = Evaluate(statement.Expr);
        if (val is null)
            throw new LoxRuntimeException(null, "Variable is null");
        Console.WriteLine(Stringify(val));
        return null;
    }

    public object VisitVarStatement(Var expression)
    {
        var value = expression.Initializer != null ? Evaluate(expression.Initializer) : null;
        _environment.Define(expression.Name.Lexeme, value);
        return null;
    }

    public object VisitWhileStatement(While statement)
    {
        while (IsTruthy(Evaluate(statement.Condition)))
        {
            try
            {
                Execute(statement.Body);
            }
            catch (BreakException)
            {
                break;
            }
            catch (ContinueException)
            {
                continue;
            }
        }

        return null;
    }

    public object VisitReturnStatement(Return statement)
    {
        var val = statement.Value != null ? Evaluate(statement.Value) : null;
        throw new ReturnException(val);
    }

    public object VisitBreakStatement(Break expression)
    {
        throw new BreakException();
    }

    public object VisitContinueStatement(Continue expression)
    {
        throw new ContinueException();
    }

    public void Resolve(IExpression expression, int depth)
    {
        _locals[expression] = depth;
    }
    
    private object? LookupVariable(Token name, Variable expression)
    {
        var distance = _locals.GetValueOrDefault(expression);
        return distance != null ? _environment.GetAt((int)distance, name.Lexeme) : Globals.Get(name);
    }
}