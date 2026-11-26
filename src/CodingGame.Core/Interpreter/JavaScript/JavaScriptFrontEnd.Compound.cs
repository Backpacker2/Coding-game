namespace CodingGame.Core.Interpreter.JavaScript;

public sealed partial class JavaScriptFrontEnd
{
    private If ParseIf()
    {
        Token keyword = Advance();
        ExpectOperator("(");
        Expression condition = ParseCondition();
        Expect(")");
        List<Statement> then = ParseBlock();

        List<Statement> otherwise = [];
        if (Current is { Kind: TokenKind.Keyword, Text: "else" } elseToken)
        {
            Advance();
            if (Current is { Kind: TokenKind.Keyword, Text: "if" })
            {
                EnterNesting(elseToken.Line);
                otherwise = [ParseIf()];
                _nestingDepth--;
            }
            else
            {
                otherwise = ParseBlock();
            }
        }

        return new If(keyword.Line, condition, then, otherwise);
    }

    private While ParseWhile()
    {
        Token keyword = Advance();
        ExpectOperator("(");
        Expression condition = ParseCondition();
        Expect(")");
        List<Statement> body = ParseBlock();
        return new While(keyword.Line, condition, body);
    }

    private Expression ParseCondition()
    {
        Expression condition = ParseExpression();
        if (IsOperator("="))
        {
            throw Failure(ScriptErrorKind.SyntaxError, Current.Line, "use === to compare; = gives a variable a value");
        }

        return condition;
    }

    private Assign ParseLet()
    {
        Token keyword = Advance();
        if (_nestingDepth > 0)
        {
            throw Failure(ScriptErrorKind.NotInGame, keyword.Line, "let inside a block");
        }

        Token name = ExpectName();
        if (!_declaredNames.Add(name.Text))
        {
            throw Failure(ScriptErrorKind.SyntaxError, name.Line, $"Identifier '{name.Text}' has already been declared");
        }

        ExpectOperator("=");
        Expression value = ParseExpression();
        ExpectSemicolon();
        return new Assign(name.Line, name.Text, value);
    }

    private Assign ParseAssignment()
    {
        Token name = ExpectName();
        if (!_declaredNames.Contains(name.Text))
        {
            throw Failure(
                ScriptErrorKind.NameOutOfScope,
                name.Line,
                $"'{name.Text}' is not declared; write let {name.Text} = ... first");
        }

        ExpectOperator("=");
        Expression value = ParseExpression();
        ExpectSemicolon();
        return new Assign(name.Line, name.Text, value);
    }

    private Repeat ParseFor()
    {
        Token keyword = Advance();
        ExpectOperator("(");
        if (Current.Kind != TokenKind.Keyword || Current.Text != "let")
        {
            throw OtherForLoops(Current);
        }

        Advance();
        string variable = ExpectName().Text;
        _declaredNames.Add(variable);
        if (!IsOperator("="))
        {
            throw Current.Kind == TokenKind.EndOfFile ? UnexpectedToken(Current) : OtherForLoops(Current);
        }

        Advance();
        if (Current.Kind != TokenKind.Number || Current.Text != "0")
        {
            throw OtherForLoops(Current);
        }

        Advance();
        if (IsOperator(","))
        {
            throw OtherForLoops(Current);
        }

        ExpectOperator(";");
        if (Current.Kind != TokenKind.Name || Current.Text != variable)
        {
            throw OtherForLoops(Current);
        }

        Advance();
        if (!IsOperator("<"))
        {
            throw OtherForLoops(Current);
        }

        Advance();
        Expression count = ParseExpression();
        ExpectOperator(";");
        if (Current.Kind != TokenKind.Name || Current.Text != variable)
        {
            throw OtherForLoops(Current);
        }

        Advance();
        if (!IsOperator("++"))
        {
            throw OtherForLoops(Current);
        }

        Advance();
        Expect(")");
        List<Statement> body = ParseBlock();
        return new Repeat(keyword.Line, count, body, variable);
    }

    private FunctionDefinition ParseFunctionDefinition()
    {
        Token keyword = Advance();
        string name = ExpectName().Text;
        ExpectOperator("(");
        if (Current.Kind == TokenKind.Name)
        {
            throw Failure(ScriptErrorKind.NotInGame, Current.Line, "parameters");
        }

        Expect(")");
        List<Statement> body = ParseBlock();
        return new FunctionDefinition(keyword.Line, name, body);
    }

    private List<Statement> ParseBlock()
    {
        if (!IsOperator("{"))
        {
            throw Current.Kind == TokenKind.EndOfFile
                ? UnexpectedToken(Current)
                : Failure(ScriptErrorKind.SyntaxError, Current.Line, "expected '{', braces are required around the body");
        }

        int openLine = Advance().Line;
        EnterNesting(openLine);
        var body = new List<Statement>();
        while (!IsOperator("}"))
        {
            ParseStatement(body);
        }

        Advance();
        _nestingDepth--;
        return HoistFunctions(body);
    }

    private Token ExpectName()
    {
        Token token = Current;
        if (token.Kind != TokenKind.Name)
        {
            throw UnexpectedToken(token);
        }

        Advance();
        return token;
    }

    // JavaScript hoists function declarations, so a call may come before its definition.
    private static List<Statement> HoistFunctions(List<Statement> statements)
    {
        var hoisted = new List<Statement>(statements.Count);
        hoisted.AddRange(statements.OfType<FunctionDefinition>());
        hoisted.AddRange(statements.Where(statement => statement is not FunctionDefinition));
        return hoisted;
    }

    private static ParseFailure OtherForLoops(Token token)
    {
        return Failure(ScriptErrorKind.NotInGame, token.Line, "other for loops");
    }
}
