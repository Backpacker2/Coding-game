using System.Globalization;

namespace CodingGame.Core.Interpreter.JavaScript;

public sealed partial class JavaScriptFrontEnd
{
    private Expression ParseExpression()
    {
        Expression expression = ParsePrimary();
        if (Current is { Kind: TokenKind.Operator, Text: "===" or "!==" } operatorToken)
        {
            Advance();
            var comparison = operatorToken.Text == "===" ? CompareOperator.Equal : CompareOperator.NotEqual;
            Expression right = ParsePrimary();
            expression = new Compare(expression.Line, expression, comparison, right);
            if (Current is { Kind: TokenKind.Operator, Text: "===" or "!==" } chained)
            {
                throw Failure(ScriptErrorKind.NotInGame, chained.Line, "chained comparisons");
            }
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
                List<Expression> arguments = ParseArguments();
                _nestingDepth--;
                return new Call(token.Line, token.Text, arguments);
            case TokenKind.Number:
                Advance();
                return new Literal(token.Line, Value.FromNumber(int.Parse(token.Text, CultureInfo.InvariantCulture)));
            case TokenKind.Keyword when token.Text is "true" or "false":
                Advance();
                return new Literal(token.Line, Value.FromBoolean(token.Text == "true"));
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
