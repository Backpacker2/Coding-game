using System.Globalization;

namespace CodingGame.Core.Interpreter.Python;

public sealed partial class PythonFrontEnd
{
    private Expression ParseExpression()
    {
        Expression left = ParsePrimary();
        Token token = Current;
        if (token.Kind != TokenKind.Operator || token.Text is not ("==" or "!="))
        {
            return left;
        }

        Advance();
        var comparison = token.Text == "==" ? CompareOperator.Equal : CompareOperator.NotEqual;
        Expression right = ParsePrimary();
        return new Compare(left.Line, left, comparison, right);
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

                Advance();
                return new Call(token.Line, token.Text, ParseArguments());
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

    private List<Expression> ParseArguments()
    {
        var arguments = new List<Expression>();
        if (IsOperator(")"))
        {
            Advance();
            return arguments;
        }

        arguments.Add(ParseExpression());
        while (IsOperator(","))
        {
            Advance();
            arguments.Add(ParseExpression());
        }

        Expect(TokenKind.Operator, ")");
        return arguments;
    }
}
