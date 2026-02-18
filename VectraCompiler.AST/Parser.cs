using VectraCompiler.AST.Lexing.Models;
using VectraCompiler.AST.Models;
using VectraCompiler.AST.Models.Declarations;
using VectraCompiler.AST.Models.Declarations.Interfaces;
using VectraCompiler.AST.Models.Expressions;
using VectraCompiler.AST.Models.Statements;
using VectraCompiler.Core;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.AST;

public sealed class Parser(List<Token> tokens, string file)
{
    private int _position;
    private readonly List<Diagnostic> _diagnostics = new();

    public ParseResult Parse()
    {
        var parsedSpace = ParseSpace();

        return new ParseResult(new VectraAstFile
        {
            FilePath = file,
            Space = parsedSpace
        }, _diagnostics.AsReadOnly());
    }

    private SpaceDeclarationNode ParseSpace(SpaceDeclarationNode? parent = null)
    {
        SpaceDeclarationNode space;

        // space is required by language rules, but we recover by defaulting to Global.
        if (!Match("space"))
        {
            Report(ErrorCode.ExpectedTokenMissing, "Expected space declaration", Peek());
            space = new SpaceDeclarationNode("Global", [], SourceSpan.EmptyAtStart, parent);
        }
        else
        {
            // Parse: space A.B.C;
            var nameToken = Consume(TokenType.Identifier, "Expected space name");
            space = new SpaceDeclarationNode(nameToken.Value, [],
                new SourceSpan(nameToken.Position, nameToken.Position), parent);

            while (!IsAtEnd() && Match("."))
            {
                var identifierToken = Consume(TokenType.Identifier, "Expected identifier after '.'");
                space = new SpaceDeclarationNode(identifierToken.Value, [],
                    new(nameToken.Position, identifierToken.Position), space);
            }

            Expect(";", "Expected ';' after space declaration");
        }

        var types = new List<ITypeDeclarationNode>();

        // Top-level loop: recover per-type so we can report multiple errors.
        while (!IsAtEnd())
        {
            var start = _position;
            try
            {
                var type = ParseTypeDeclaration();
                types.Add(type);
            }
            catch (ParseError)
            {
                SynchronizeTopLevel();
            }

            // Absolute safety: never allow no-progress loops.
            if (_position == start)
                Advance(); // consume something to avoid an infinite loop
        }

        space.AddTypes(types);

        while (space.Parent is not null)
            space = space.Parent;

        return space;
    }

    private ITypeDeclarationNode ParseTypeDeclaration()
    {
        var start = Peek();

        if (start.Type != TokenType.Keyword)
            throw Error(ErrorCode.ExpectedTokenMissing, "Expected a keyword to start a type declaration", start);

        // We consume the keyword once we know it is a keyword.
        var typeToken = Advance();

        return typeToken.Value switch
        {
            "class" => ParseClassDeclaration(),
            _ => throw Error(ErrorCode.UnknownType, $"Unexpected type keyword: '{typeToken.Value}'", typeToken)
        };
    }

    private ClassDeclarationNode ParseClassDeclaration()
    {
        var nameToken = Consume(TokenType.Identifier, "Expected a class name");
        Expect("{", "Expected '{' to start class body");

        var members = new List<IMemberNode>();

        // Member loop: recover per-member.
        while (!IsAtEnd() && !Check("}"))
        {
            var start = _position;
            try
            {
                var member = ParseMemberDeclaration(nameToken);
                members.Add(member);
            }
            catch (ParseError)
            {
                SynchronizeClassMember();
            }

            if (_position == start)
                Advance();
        }

        // Consume closing brace if present; otherwise report a missing brace at EOF.
        if (!Match("}"))
        {
            if (IsAtEnd())
                Report(ErrorCode.UnexpectedEndOfFile, "Missing '}' to close class body", PreviousOrPeek());
            else
                Report(ErrorCode.ExpectedTokenMissing, "Expected '}' to close class body", Peek());
        }

        return new ClassDeclarationNode(
            nameToken.Value,
            members,
            new SourceSpan(
                nameToken.Position.Line,
                nameToken.Position.Column,
                PreviousOrPeek().Position.Line,
                PreviousOrPeek().Position.Column
            ));
    }

    private IMemberNode ParseMemberDeclaration(Token classToken)
    {
        // TODO: add support for modifiers
        if (!Check(TokenType.Identifier, TokenType.Keyword))
            throw Error(ErrorCode.ExpectedTokenMissing, "Expected identifier or keyword at start of member declaration", Peek());

        var typeToken = Advance();

        if (typeToken.Value == classToken.Value && Match("("))
            return ParseConstructor(classToken);

        var nameToken = Consume(TokenType.Identifier, "Expected an identifier after type token");

        if (Match("("))
            return ParseMethod(typeToken, nameToken);

        if (Match("=") || Match(";"))
            return ParseField(typeToken, nameToken);

        if (Match("{"))
            return ParseProperty(typeToken, nameToken);

        throw Error(ErrorCode.UnknownType, "Unknown member declaration", PreviousOrPeek());
    }

    private ConstructorDeclarationNode ParseConstructor(Token classToken)
    {
        var parameters = new List<VParameter>();

        // Parse parameter list. If it's malformed, recover to ')' or '{'.
        if (!Match(")"))
        {
            try
            {
                do
                {
                    var typeToken = ConsumeTypeToken("Expected parameter type");
                    var nameToken = Consume(TokenType.Identifier, "Expected parameter name");
                    parameters.Add(new VParameter(nameToken.Value, typeToken.Value, new SourceSpan(typeToken.Position, nameToken.Position)));
                } while (Match(","));

                // Common recovery for your example: if '{' appears, treat ')' as missing.
                if (Check("{"))
                {
                    Report(ErrorCode.ExpectedTokenMissing, "Expected ')' after parameter list", Peek());
                }
                else
                {
                    Expect(")", "Expected ')' after parameter list");
                }
            }
            catch (ParseError)
            {
                SynchronizeToAny(")", "{");
                Match(")"); // consume if we landed on it
            }
        }

        var bodyStart = Consume("{", "Expected '{' to start method body");

        var statements = new List<IStatementNode>();

        while (!IsAtEnd() && !Check("}"))
        {
            var start = _position;
            try
            {
                var statement = ParseStatement();
                statements.Add(statement);
            }
            catch (ParseError)
            {
                SynchronizeStatement();
            }

            if (_position == start)
                Advance();
        }

        if (!Match("}"))
        {
            if (IsAtEnd())
                Report(ErrorCode.UnexpectedEndOfFile, "Missing '}' to close constructor body", PreviousOrPeek());
            else
                Report(ErrorCode.ExpectedTokenMissing, "Expected '}' to close constructor body", Peek());
        }

        var body = new BlockStatementNode(new SourceSpan(bodyStart.Position, PreviousOrPeek().Position))
        {
            Statements = statements
        };

        return new ConstructorDeclarationNode(
            classToken.Value,
            parameters,
            body,
            new SourceSpan(
                classToken.Position.Line,
                classToken.Position.Column,
                PreviousOrPeek().Position.Line,
                PreviousOrPeek().Position.Column
            ));
    }

    private MethodDeclarationNode ParseMethod(Token typeToken, Token nameToken)
    {
        var parameters = new List<VParameter>();

        if (!Match(")"))
        {
            try
            {
                do
                {
                    var pTypeToken = ConsumeTypeToken("Expected parameter type");
                    var pNameToken = Consume(TokenType.Identifier, "Expected parameter name");
                    parameters.Add(new(pNameToken.Value, pTypeToken.Value, new SourceSpan(pTypeToken.Position, pNameToken.Position)));
                } while (Match(","));

                // Your common recovery: "Expected ')', found '{'"
                if (Check("{"))
                {
                    Report(ErrorCode.ExpectedTokenMissing, "Expected ')' after parameter list", Peek());
                }
                else
                {
                    Expect(")", "Expected ')' after parameter list");
                }
            }
            catch (ParseError)
            {
                SynchronizeToAny(")", "{");
                Match(")");
            }
        }

        var bodyStart = Consume("{", "Expected '{' to start method body");

        var statements = new List<IStatementNode>();

        while (!IsAtEnd() && !Check("}"))
        {
            var start = _position;
            try
            {
                var statement = ParseStatement();
                statements.Add(statement);
            }
            catch (ParseError)
            {
                SynchronizeStatement();
            }

            if (_position == start)
                Advance();
        }

        if (!Match("}"))
        {
            if (IsAtEnd())
                Report(ErrorCode.UnexpectedEndOfFile, "Missing '}' to close method body", PreviousOrPeek());
            else
                Report(ErrorCode.ExpectedTokenMissing, "Expected '}' to close method body", Peek());
        }
        
        var body = new BlockStatementNode(new SourceSpan(bodyStart.Position, PreviousOrPeek().Position))
        {
            Statements = statements
        };


        return new MethodDeclarationNode(
            nameToken.Value,
            parameters,
            body,
            typeToken.Value,
            new SourceSpan(
                typeToken.Position.Line,
                typeToken.Position.Column,
                PreviousOrPeek().Position.Line,
                PreviousOrPeek().Position.Column
            ));
    }

    private FieldDeclarationNode ParseField(Token typeToken, Token nameToken)
    {
        // ';' or '=' was already parsed
        if (Previous().Value == ";")
            return new FieldDeclarationNode(
                nameToken.Value, typeToken.Value, null,
                new SourceSpan(typeToken.Position.Line, typeToken.Position.Column,
                    Previous().Position.Line, Previous().Position.Column));

        var initializer = ParseExpression();

        // If '}' comes next, a semicolon is almost certainly missing (your error #3 style).
        if (Check("}"))
        {
            Report(ErrorCode.ExpectedTokenMissing, "Expected ';' after field initializer", Peek());
        }
        else
        {
            Expect(";", "Expected ';' after field initializer");
        }

        return new FieldDeclarationNode(
            nameToken.Value, typeToken.Value, initializer,
            new SourceSpan(typeToken.Position.Line, typeToken.Position.Column,
                PreviousOrPeek().Position.Line, PreviousOrPeek().Position.Column));
    }

    private PropertyDeclarationNode ParseProperty(Token typeToken, Token nameToken)
    {
        var hasGetter = false;
        var hasSetter = false;

        while (!IsAtEnd() && !Check("}"))
        {
            var accessorType = Consume(TokenType.Keyword,
                "Expected 'get' or 'set' after start of property declaration");

            switch (accessorType.Value)
            {
                case "get":
                    if (hasGetter)
                        throw Error(ErrorCode.DuplicateAccessor, "Properties can only have one getter.", accessorType);
                    hasGetter = true;
                    break;
                case "set":
                    if (hasSetter)
                        throw Error(ErrorCode.DuplicateAccessor, "Properties can only have one setter.", accessorType);
                    hasSetter = true;
                    break;
                default:
                    throw Error(ErrorCode.UnexpectedToken, $"Unexpected token: '{accessorType.Value}' in property body", accessorType);
            }

            // Missing ';' recovery if next is '}'.
            if (Check("}"))
            {
                Report(ErrorCode.ExpectedTokenMissing, "Expected ';' after property accessor", Peek());
            }
            else
            {
                Expect(";", "Expected ';' after property accessor");
            }
        }

        if (!Match("}"))
        {
            if (IsAtEnd())
                Report(ErrorCode.UnexpectedEndOfFile, "Missing '}' to close property body", PreviousOrPeek());
            else
                Report(ErrorCode.ExpectedTokenMissing, "Expected '}' to close property body", Peek());
        }

        return new PropertyDeclarationNode(
            nameToken.Value,
            typeToken.Value,
            new SourceSpan(typeToken.Position.Line, typeToken.Position.Column,
                PreviousOrPeek().Position.Line, PreviousOrPeek().Position.Column),
            hasGetter,
            hasSetter
        );
    }

    private IStatementNode ParseStatement()
    {
        if (Match("return"))
            return ParseReturnStatement();
        if (Check("let"))
            return ParseVariableDeclarationStatement(false);
        if (Check(TokenType.Identifier, TokenType.Keyword) && PeekNext()!.Type == TokenType.Identifier)
            return ParseVariableDeclarationStatement(true);
        return ParseExpressionStatement();
    }

    private ReturnStatementNode ParseReturnStatement()
    {
        var startPosition = Previous().Position;

        if (Match(";"))
            return new ReturnStatementNode(new SourceSpan(startPosition.Line, startPosition.Column,
                Previous().Position.Line, Previous().Position.Column));

        var value = ParseExpression();

        if (Check("}"))
        {
            Report(ErrorCode.ExpectedTokenMissing, "Expected ';' after return statement", Peek());
        }
        else
        {
            Expect(";", "Expected ';' after return statement");
        }

        return new ReturnStatementNode(value, new SourceSpan(startPosition.Line, startPosition.Column,
            PreviousOrPeek().Position.Line, PreviousOrPeek().Position.Column));
    }

    private VariableDeclarationStatementNode ParseVariableDeclarationStatement(bool isExplicit)
    {
        string? explicitType = null;
        var typeToken = Peek();

        if (isExplicit)
            explicitType = Advance().Value;
        else
            Advance(); // 'let'

        var nameToken = Consume(TokenType.Identifier, "Expected identifier.");
        var name = nameToken.Value;

        IExpressionNode? initializer = null;
        if (Match("="))
            initializer = ParseExpression();

        if (!isExplicit && initializer is null)
            throw Error(ErrorCode.InvalidVariableDeclaration, "Implicit variable declaration requires an initializer", typeToken);

        // Missing ';' recovery if next is '}'.
        if (Check("}"))
        {
            Report(ErrorCode.ExpectedTokenMissing, "Expected ';' after variable declaration", Peek());
        }
        else
        {
            Expect(";", "Expected ';' after variable declaration");
        }

        return new VariableDeclarationStatementNode(
            name,
            explicitType,
            initializer,
            new SourceSpan(
                typeToken.Position.Line,
                typeToken.Position.Column,
                PreviousOrPeek().Position.Line,
                PreviousOrPeek().Position.Column
            ));
    }

    private ExpressionStatementNode ParseExpressionStatement()
    {
        var expr = ParseExpression();

        if (Check("}"))
        {
            Report(ErrorCode.ExpectedTokenMissing, "Expected ';' after expression", Peek());
        }
        else
        {
            Expect(";", "Expected ';' after expression");
        }

        return new ExpressionStatementNode(expr, expr.Span);
    }

    private IExpressionNode ParseExpression() => ParseAssignment();

    private IExpressionNode ParseAssignment()
    {
        var left = ParseBinary();

        if (Match("="))
        {
            var right = ParseExpression();
            left = new AssignmentExpressionNode(left, right,
                left.Span with { EndLine = right.Span.EndLine, EndColumn = right.Span.EndColumn });
        }

        return left;
    }

    private IExpressionNode ParseBinary()
    {
        var left = ParsePrimary();

        while (IsBinaryOperator(Peek().Value))
        {
            var opToken = Advance();
            var right = ParsePrimary();
            left = new BinaryExpressionNode(opToken.Value, left, right,
                new SourceSpan(left.Span.StartLine, left.Span.StartColumn, right.Span.EndLine, right.Span.EndColumn));
        }

        return left;
    }

    private IExpressionNode ParsePrimary()
    {
        if (IsAtEnd())
            throw Error(ErrorCode.UnexpectedEndOfFile, "Unexpected end of file in expression", PreviousOrPeek());

        var token = Advance();

        return token.Type switch
        {
            TokenType.Number or TokenType.String => ParseLiteralExpression(token),
            TokenType.Keyword when token.Value == "true" || token.Value == "false" => ParseLiteralExpression(token),
            TokenType.Identifier => ParsePostfix(new IdentifierExpressionNode(token.Value,
                new SourceSpan(token.Position, Peek().Position))),

            TokenType.Keyword when token.Value == "this" => ParsePostfix(
                new IdentifierExpressionNode("this", new(token.Position, Peek().Position))),

            TokenType.Keyword when token.Value == "new" => ParseNewExpression(token),

            _ => throw Error(ErrorCode.UnexpectedToken, $"Unexpected token '{token.Value}' in expression", token)
        };
    }

    private LiteralExpressionNode ParseLiteralExpression(Token token)
    {
        object value = token.Type switch
        {
            TokenType.Number when !token.Value.Contains('.') => int.Parse(token.Value),
            TokenType.Number when token.Value.Contains('.') => double.Parse(token.Value),
            TokenType.String => token.Value,
            TokenType.Keyword when token.Value == "true" => true,
            TokenType.Keyword when token.Value == "false" => false,
            _ => throw Error(ErrorCode.UnexpectedToken, $"Unexpected token '{token.Value}' in literal expression", token)
        };
        return new LiteralExpressionNode(value, new SourceSpan(token.Position, Peek().Position));
    }

    private IExpressionNode ParsePostfix(IExpressionNode expr)
    {
        while (true)
        {
            if (Match("."))
            {
                var name = Consume(TokenType.Identifier, "Expected identifier after '.'.");
                expr = new MemberAccessExpressionNode(
                    expr,
                    name.Value,
                    expr.Span with { EndLine = name.Position.Line, EndColumn = name.Position.Column });
                continue;
            }

            if (Match("("))
            {
                var args = new List<IExpressionNode>();

                if (!Match(")"))
                {
                    do
                    {
                        args.Add(ParseExpression());
                    } while (Match(","));

                    // If we see a statement/block boundary, assume ')' missing.
                    if (Check(";") || Check("}") || Check("{"))
                        Report(ErrorCode.ExpectedTokenMissing, "Expected ')' after arguments", Peek());
                    else
                        Expect(")", "Expected ')' after arguments");
                }

                expr = new CallExpressionNode(
                    expr,
                    args,
                    expr.Span with { EndLine = PreviousOrPeek().Position.Line, EndColumn = PreviousOrPeek().Position.Column });

                continue;
            }

            break;
        }

        return expr;
    }

    private NewExpressionNode ParseNewExpression(Token token)
    {
        var typeToken = Consume(TokenType.Identifier, "Expected type name after 'new'");
        Expect("(", "Expected '(' after type name");

        var args = new List<IExpressionNode>();

        if (Match(")"))
            return new NewExpressionNode(typeToken.Value, args,
                new SourceSpan(token.Position, Previous().Position));

        do
        {
            args.Add(ParseExpression());
        } while (Match(","));

        if (Check(";") || Check("}") || Check("{"))
            Report(ErrorCode.ExpectedTokenMissing, "Expected ')' after arguments", Peek());
        else
            Expect(")", "Expected ')' after arguments");

        return new NewExpressionNode(typeToken.Value, args, new SourceSpan(token.Position, PreviousOrPeek().Position));
    }

    #region Recovery / Diagnostics

    private void Report(ErrorCode code, string message, Token token)
    {
        _diagnostics.Add(new Diagnostic(
            code,
            Severity.Error,
            message,
            Path.GetFileName(file),
            token.Position.Line,
            token.Position.Column
        ));
    }

    private ParseError Error(ErrorCode code, string message, Token token)
    {
        Report(code, message, token);
        return new ParseError(message);
    }

    /// <summary>
    /// Skip tokens until we�re likely at the start of the next top-level declaration.
    /// </summary>
    private void SynchronizeTopLevel()
    {
        while (!IsAtEnd())
        {
            if (Check(TokenType.Keyword) && Peek().Value == "class")
                return;

            // If we hit '}', it might close something above us; let callers handle it.
            if (Check("}"))
                return;

            Advance();
        }
    }

    /// <summary>
    /// Skip tokens until we can plausibly start a new member, or until end of class body.
    /// </summary>
    private void SynchronizeClassMember()
    {
        while (!IsAtEnd())
        {
            if (Check("}"))
                return;

            // member starts look like: (keyword|identifier) identifier ...
            if (Check(TokenType.Keyword, TokenType.Identifier) &&
                PeekOffset(1)?.Type == TokenType.Identifier)
                return;

            // statement-ish boundary
            if (Match(";"))
                return;

            Advance();
        }
    }

    /// <summary>
    /// Skip tokens until ';' or '}' (end of statement or block).
    /// </summary>
    private void SynchronizeStatement()
    {
        while (!IsAtEnd())
        {
            if (Match(";"))
                return;

            if (Check("}"))
                return;

            Advance();
        }
    }

    private void SynchronizeToAny(params string[] lexemes)
    {
        var set = new HashSet<string>(lexemes);
        while (!IsAtEnd() && !set.Contains(Peek().Value))
            Advance();
    }

    #endregion

    #region Utility

    private bool IsAtEnd() => Peek().Type == TokenType.EndOfFile;
    private Token Peek() => tokens[_position];
    private Token? PeekNext() => _position + 1 < tokens.Count ? tokens[_position + 1] : null;
    private Token? PeekOffset(int offset) => _position + offset < tokens.Count ? tokens[_position + offset] : null;

    private Token Advance() => tokens[_position++];

    private Token Previous() => tokens[_position - 1];

    private Token PreviousOrPeek()
    {
        if (_position > 0) return Previous();
        return Peek();
    }

    private bool Match(string lexeme)
    {
        if (IsAtEnd()) return false;
        if (Peek().Value != lexeme) return false;
        Advance();
        return true;
    }

    private void Expect(string lexeme, string errorMessage)
    {
        if (Match(lexeme)) return;
        throw Error(ErrorCode.ExpectedTokenMissing, $"{errorMessage}. Expected '{lexeme}'", Peek());
    }

    private Token Consume(TokenType type, string errorMessage)
    {
        if (IsAtEnd())
            throw Error(ErrorCode.UnexpectedEndOfFile, $"Expected token of type: {type}, but reached end of file.", PreviousOrPeek());

        if (Peek().Type != type)
            throw Error(ErrorCode.ExpectedTokenMissing, $"{errorMessage}. Expected {type}", Peek());

        return Advance();
    }

    private Token Consume(string lexeme, string errorMessage)
    {
        if (IsAtEnd())
            throw Error(ErrorCode.UnexpectedEndOfFile, $"Expected token: '{lexeme}', but reached end of file.", PreviousOrPeek());
        if (Peek().Value != lexeme)
            throw Error(ErrorCode.ExpectedTokenMissing, $"{errorMessage}. Expected '{lexeme}'", Peek());
        return Advance();
    }

    private static bool IsTypeKeyword(string kw) => kw is "number" or "bool" or "string";

    private Token ConsumeTypeToken(string errorMessage)
    {
        if (IsAtEnd())
            throw Error(ErrorCode.UnexpectedEndOfFile, "Unexpected end of file. " + errorMessage, PreviousOrPeek());
        var t = Peek();
        if (t.Type == TokenType.Identifier)
            return Advance();

        if (t.Type == TokenType.Keyword && IsTypeKeyword(t.Value))
            return Advance();
        throw Error(ErrorCode.ExpectedTokenMissing, $"{errorMessage}", t);
    }

    private bool Check(params TokenType[] types) => types.Contains(Peek().Type);
    private bool Check(string lexeme) => Peek().Value == lexeme;

    private static bool IsBinaryOperator(string lexeme) =>
        lexeme is "+" or "-" or "*" or "/" or "==" or "!=" or ">" or "<" or ">=" or "<=";

    #endregion
}

public sealed class ParseError(string message) : Exception(message);
