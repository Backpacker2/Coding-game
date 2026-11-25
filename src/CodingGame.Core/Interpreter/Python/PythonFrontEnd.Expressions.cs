using System.Globalization;

namespace CodingGame.Core.Interpreter.Python;

public sealed partial class PythonFrontEnd
{
    private Expression ParseExpression()
    {
        Expression expression = ParsePrimary();
        Token token = Current;
        if (token is { Kind: TokenKind.Operator, Text: "==" or "!=" })
        {
            Advance();
            var comparison = token.Text == "==" ? CompareOperator.Equal : CompareOperator.NotEqual;
            Expression right = ParsePrimary();
            expression = new Compare(expression.Line, expression, comparison, right);
            if (Current is { Kind: TokenKind.Operator, Text: "==" or "!=" } chained)
            {
                throw Failure(ScriptErrorKind.NotUnlocked, chained.Line, "chained comparisons");
            }
        }

        if (Current.Kind == TokenKind.Keyword)
        {
            throw UnexpectedToken(Current);
        }

        return expression;
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
                List<Expression> arguments = ParseArguments(open.Line);
                _nestingDepth--;
                return new Call(token.Line, token.Text, arguments);
            case TokenKind.Number:
                Advance();
                return new Literal(token.Line, Value.FromNumber(int.Parse(token.Text, CultureInfo.InvariantCulture)));
            case TokenKind.Keyword when token.Text is "True" or "False":
                Advance();
                return new Literal(token.Line, Value.FromBoolean(token.Text == "True"));
            default:
                throw UnexpectedToken(token);
        }
    }

    private List<Expression> ParseArguments(int openLine)
    {
        var arguments = new List<Expression>();
        if (IsOperator(")"))
        {
            Advance();
            return arguments;
        }

        arguments.Add(ParseArgument(openLine));
        while (IsOperator(","))
        {
            Advance();
            arguments.Add(ParseArgument(openLine));
        }

        if (Current.Kind == TokenKind.Newline)
        {
            throw Failure(ScriptErrorKind.SyntaxError, openLine, "'(' was never closed");
        }

        Expect(TokenKind.Operator, ")");
        return arguments;
    }

    private Expression ParseArgument(int openLine)
    {
        if (Current.Kind == TokenKind.Newline)
        {
            throw Failure(ScriptErrorKind.SyntaxError, openLine, "'(' was never closed");
        }

        return ParseExpression();
    }
}
