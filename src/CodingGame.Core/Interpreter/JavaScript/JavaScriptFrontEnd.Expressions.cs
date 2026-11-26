using System.Globalization;

namespace CodingGame.Core.Interpreter.JavaScript;

public sealed partial class JavaScriptFrontEnd
{
    private Expression ParseExpression()
    {
        Expression expression = ParseSum();
        if (Current is { Kind: TokenKind.Operator, Text: "===" or "!==" or "<" or ">" or "<=" or ">=" } operatorToken)
        {
            Advance();
            CompareOperator comparison = operatorToken.Text switch
            {
                "===" => CompareOperator.Equal,
                "!==" => CompareOperator.NotEqual,
                "<" => CompareOperator.Less,
                ">" => CompareOperator.Greater,
                "<=" => CompareOperator.LessOrEqual,
                _ => CompareOperator.GreaterOrEqual,
            };
            Expression right = ParseSum();
            expression = new Compare(expression.Line, expression, comparison, right);
            if (Current is { Kind: TokenKind.Operator, Text: "===" or "!==" or "<" or ">" or "<=" or ">=" } chained)
            {
                throw Failure(ScriptErrorKind.NotInGame, chained.Line, "chained comparisons");
            }
        }

        return expression;
    }

    private Expression ParseSum()
    {
        Expression expression = ParseProduct();
        while (Current is { Kind: TokenKind.Operator, Text: "+" or "-" } token)
        {
            Advance();
            Expression right = ParseProduct();
            ArithmeticOperator arithmeticOperator = token.Text == "+" ? ArithmeticOperator.Add : ArithmeticOperator.Subtract;
            expression = new Arithmetic(expression.Line, expression, arithmeticOperator, right);
        }

        return expression;
    }

    private Expression ParseProduct()
    {
        Expression expression = ParseUnary();
        while (IsOperator("*"))
        {
            Advance();
            Expression right = ParseUnary();
            expression = new Arithmetic(expression.Line, expression, ArithmeticOperator.Multiply, right);
        }

        return expression;
    }

    private Expression ParseUnary()
    {
        if (!IsOperator("-"))
        {
            return ParsePrimary();
        }

        Token minus = Advance();
        EnterNesting(minus.Line);
        Expression operand = ParseUnary();
        _nestingDepth--;
        return new Negate(minus.Line, operand);
    }

    private Expression ParsePrimary()
    {
        Token token = Current;
        switch (token.Kind)
        {
            case TokenKind.Name:
                Advance();
                if (!IsOperator("("))
                {
                    return new Name(token.Line, token.Text);
                }

                Token open = Advance();
                EnterNesting(open.Line);
                List<Expression> arguments = ParseArguments();
                _nestingDepth--;
                return new Call(token.Line, token.Text, arguments);
            case TokenKind.Number:
                Advance();
                return new Literal(token.Line, Value.FromNumber(int.Parse(token.Text, CultureInfo.InvariantCulture)));
            case TokenKind.Keyword when token.Text is "true" or "false":
                Advance();
                return new Literal(token.Line, Value.FromBoolean(token.Text == "true"));
            case TokenKind.Operator when token.Text == "(":
                Advance();
                EnterNesting(token.Line);
                Expression grouped = ParseExpression();
                _nestingDepth--;
                Expect(")");
                return grouped;
            default:
                throw UnexpectedToken(token);
        }
    }

    private List<Expression> ParseArguments()
    {
        var arguments = new List<Expression>();
        if (!IsOperator(")"))
        {
            arguments.Add(ParseExpression());
            while (IsOperator(","))
            {
                Advance();
                arguments.Add(ParseExpression());
            }
        }

        Expect(")");
        return arguments;
    }
}
