using VectraCompiler.AST.Lexing.Models;
using VectraCompiler.AST.Models;
using VectraCompiler.AST.Models.Declarations;
using VectraCompiler.AST.Models.Declarations.Interfaces;
using VectraCompiler.AST.Models.Expressions;
using VectraCompiler.AST.Models.Statements;
using VectraCompiler.Package.Models;

namespace VectraCompiler.AST;

public class Parser(List<Token> tokens, ModuleMetadata moduleMetadata)
{
    private int _position;

    public VectraAstModule Parse()
    {
        var parsedSpace = ParseSpace();

        return new VectraAstModule
        {
            IsExecutable = moduleMetadata.Type == ModuleType.Executable,
            Name = moduleMetadata.Name,
            Space = parsedSpace
        };
    }

    private SpaceDeclarationNode ParseSpace(SpaceDeclarationNode? parent = null)
    {
        Expect("space", "Expected space declaration");

        var nameToken = Consume(TokenType.Identifier, "Expected space name");
        var space = new SpaceDeclarationNode(nameToken.Value, [],
            new SourceSpan(nameToken.Position, nameToken.Position), parent);
        while (!IsAtEnd() && Match("."))
        {
            var identifierToken = Consume(TokenType.Identifier, "Expected identifier after '.'");
            space = new SpaceDeclarationNode(identifierToken.Value, [],
                new(nameToken.Position, identifierToken.Position), space);
        }

        Expect(";", "Expected ';' after space declaration");

        var types = new List<ITypeDeclarationNode>();
        while (!IsAtEnd())
        {
            var type = ParseTypeDeclaration();
            types.Add(type);
        }

        space.AddTypes(types);
        return space;
    }

    private ITypeDeclarationNode ParseTypeDeclaration()
    {
        var typeToken = Consume(TokenType.Keyword, "Expected a keyword");
        return typeToken.Value switch
        {
            "class" => ParseClassDeclaration(),
            _ => throw new Exception(
                $"Unexpected type: {typeToken.Value} at line {typeToken.Position.Line}:{typeToken.Position.Column}")
        };
    }

    private ClassDeclarationNode ParseClassDeclaration()
    {
        var nameToken = Consume(TokenType.Identifier, "Expected a class name");
        Expect("{", "Expected '{' to start class body");
        var members = new List<IMemberNode>();
        while (!IsAtEnd() && !Match("}"))
        {
            var member = ParseMemberDeclaration(nameToken);
            members.Add(member);
        }
        
        return new ClassDeclarationNode(nameToken.Value, members, new SourceSpan(
            nameToken.Position.Line,
            nameToken.Position.Column,
            Previous().Position.Line,
            Previous().Position.Column
            ));
    }

    private IMemberNode ParseMemberDeclaration(Token classToken)
    {
        // TODO: add support for modifiers
        if (!Check(TokenType.Identifier, TokenType.Keyword))
            throw new Exception($"Expected identifier or keyword at line: {Previous().Position.Line}:{Previous().Position.Column}");
        var typeToken = Advance();
        if (typeToken.Value == classToken.Value && Match("("))
            return ParseConstructor(classToken);
        var nameToken = Consume(TokenType.Identifier, "Expected an identifier after type token");
        if (Match("("))
            return ParseMethod(typeToken, nameToken);
        if (Match("=") || Match(";"))
            return ParseField(typeToken, nameToken);
        return Match("{") ? ParseProperty(typeToken, nameToken) : throw new Exception($"Unknown member declaration at line: {Previous().Position.Line}:{Previous().Position.Column}");
    }

    private ConstructorDeclarationNode ParseConstructor(Token classToken)
    {
        var parameters = new List<VParameter>();
        if (!Match(")"))
        {
            do
            {
                var typeToken = Advance();
                var nameToken = Advance();
                parameters.Add(new VParameter(nameToken.Value, typeToken.Value));
            } while (Match(","));
            Expect(")", "Expected ')' after parameter list");
        }
        
        Expect("{", "Expected '{' to start method body");
        var statements = new List<IStatementNode>();
        while (!IsAtEnd() && !Match("}"))
        {
            var statement = ParseStatement();
            statements.Add(statement);
        }

        return new ConstructorDeclarationNode(
            classToken.Value,
            parameters,
            statements,
            new SourceSpan(
                classToken.Position.Line,
                classToken.Position.Column,
                Previous().Position.Line,
                Previous().Position.Column
            ));
    }

    private MethodDeclarationNode ParseMethod(Token typeToken, Token nameToken)
    {
        var parameters = new List<VParameter>();
        if (!Match(")"))
        {
            do
            {
                var pTypeToken = Advance();
                var pNameToken = Advance();
                parameters.Add(new(pNameToken.Value, pTypeToken.Value));
            } while (Match(","));
            Expect(")", "Expected '(' after parameter list");
        }
        
        Expect("{", "Expected '{' to start method body");
        var statements = new List<IStatementNode>();
        while (!IsAtEnd() && !Match("}"))
        {
            var statement = ParseStatement();
            statements.Add(statement);
        }

        return new MethodDeclarationNode(
            nameToken.Value,
            parameters,
            statements,
            typeToken.Value,
            new SourceSpan(
                typeToken.Position.Line,
                typeToken.Position.Column,
                Previous().Position.Line,
                Previous().Position.Column
            ));
    }

    private FieldDeclarationNode ParseField(Token typeToken, Token nameToken)
    {
        if (Match(";"))
            return new FieldDeclarationNode(
                nameToken.Value, typeToken.Value, null,
                new SourceSpan(typeToken.Position.Line, typeToken.Position.Column,
                    Previous().Position.Line, Previous().Position.Column));
        Expect("=", "Expected '=' after field declaration");
        var initializer = ParseExpression();
        Expect(";", "Expected ';' after field initializer");
        return new FieldDeclarationNode(
            nameToken.Value, typeToken.Value, initializer,
            new SourceSpan(typeToken.Position.Line, typeToken.Position.Column,
                Previous().Position.Line, Previous().Position.Column));
    }

    private PropertyDeclarationNode ParseProperty(Token typeToken, Token nameToken)
    {
        var hasGetter = false;
        var hasSetter = false;

        while (!IsAtEnd() && !Match("}"))
        {
            var accessorType = Consume(TokenType.Keyword,
                "Expected 'get' or 'set' after start of property declaration");
            switch (accessorType.Value)
            {
                case "get":
                    if (hasGetter)
                        throw new Exception("Properties can only have one getter.");
                    hasGetter = true;
                    break;
                case "set":
                    if (hasSetter)
                        throw new Exception("Properties can only have one setter.");
                    hasSetter = true;
                    break;
                default:
                    throw new Exception(
                        $"Unexpected token: '{accessorType.Value}' at line {accessorType.Position.Line}:{accessorType.Position.Column}");
            }
            Expect(";", "Expected ';' after property accessor");
        }

        return new PropertyDeclarationNode(nameToken.Value, typeToken.Value,
            new SourceSpan(typeToken.Position.Line, typeToken.Position.Column,
                Previous().Position.Line, Previous().Position.Column), hasGetter, hasSetter);
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
        Expect(";", "Expected ';' after return statement");
        return new ReturnStatementNode(value, new SourceSpan(startPosition.Line, startPosition.Column,
            Previous().Position.Line, Previous().Position.Column));
    }

    private VariableDeclarationStatementNode ParseVariableDeclarationStatement(bool isExplicit)
    {
        string? explicitType = null;
        var typeToken = Peek();
        if (isExplicit)
        {
            explicitType = Advance().Value;
        }
        else
        {
            Advance();
        }

        var nameToken = Consume(TokenType.Identifier, "Expected identifier.");
        var name = nameToken.Value;
        IExpressionNode? initializer = null;
        if (Match("="))
            initializer = ParseExpression();
        if (!isExplicit && initializer is null)
            throw new Exception(
                $"Implicit variable declaration requires that an initializer is declared at line: {typeToken.Position.Line}:{typeToken.Position.Column}");
        Expect(";", "Expected ';' after variable declaration");
        return new VariableDeclarationStatementNode(name, explicitType, initializer, new SourceSpan(typeToken.Position.Line,
            typeToken.Position.Column,
            Previous().Position.Line, Previous().Position.Column));
    }

    private ExpressionStatementNode ParseExpressionStatement()
    {
        var expr = ParseExpression();
        Expect(";", "Expected ';' after expression");
        return new ExpressionStatementNode(expr, expr.Span);
    }

    private IExpressionNode ParseExpression()
    {
        return ParseBinary();
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
        var token = Advance();
        return token.Type switch
        {
            TokenType.Number or TokenType.String => new LiteralExpressionNode(token.Value,
                new SourceSpan(token.Position, Peek().Position)),
            TokenType.Identifier => ParsePossibleCall(new IdentifierExpressionNode(token.Value,
                new SourceSpan(token.Position, Peek().Position))),
            TokenType.Keyword when token.Value == "this" => ParsePossibleCall(
                new IdentifierExpressionNode("this", new(token.Position, Peek().Position))),
            TokenType.Keyword when token.Value == "new" => ParseNewExpression(token),
            _ => throw new Exception(
                $"Unexpected token '{token.Value}' at line {token.Position.Line}:{token.Position.Column}")
        };
    }

    private IExpressionNode ParsePossibleCall(IExpressionNode target)
    {
        if (Peek().Value != "." || PeekNext()?.Type != TokenType.Identifier || PeekOffset(2)?.Value != "(")
            return target;

        Advance();
        var methodNameToken = Consume(TokenType.Identifier, "Expected identifier.");
        var arguments = new List<IExpressionNode>();
        Advance();
        if (!Match(")"))
        {
            do
            {
                arguments.Add(ParseExpression());
            } while (Match(","));
            Expect(")", "Expected ')' after arguments");
        }

        return new CallExpressionNode(target, methodNameToken.Value, arguments,
            target.Span with { EndLine = Previous().Position.Line, EndColumn = Previous().Position.Column });
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
        Expect(")", "Expected ')' after arguments");

        return new NewExpressionNode(typeToken.Value, args, new SourceSpan(token.Position, Previous().Position));
    }

    #region Utility

    private bool IsAtEnd() => Peek().Type == TokenType.EndOfFile;
    private Token Peek() => tokens[_position];
    private Token? PeekNext() => _position + 1 < tokens.Count ? tokens[_position + 1] : null;
    private Token? PeekOffset(int offset) => _position + offset < tokens.Count ? tokens[_position + offset] : null;
    private Token Advance() => tokens[_position++];
    private Token Previous() => tokens[_position - 1];

    private bool Match(string lexeme)
    {
        if (IsAtEnd()) return false;
        if (Peek().Value != lexeme) return false;
        Advance();
        return true;
    }

    private void Expect(string lexeme, string errorMessage)
    {
        if (!Match(lexeme)) 
            throw new Exception($"{errorMessage} at line: {Peek().Position.Line}:{Peek().Position.Column}");
    }

    private Token Consume(TokenType type, string errorMessage)
    {
        if (IsAtEnd())
            throw new Exception($"Expected token of type: {type}, but reached end of file.");
        return Peek().Type != type ? throw new Exception($"{errorMessage} at line: {Peek().Position.Line}:{Peek().Position.Column}") : Advance();
    }

    private bool Check(params TokenType[] types) => types.Contains(Peek().Type);
    private bool Check(string lexeme) => Peek().Value == lexeme;
    private static bool IsBinaryOperator(string lexeme) =>
        lexeme is "+" or "-" or "*" or "/" or "==" or "!=" or ">" or "<" or ">=" or "<=";

    #endregion
}