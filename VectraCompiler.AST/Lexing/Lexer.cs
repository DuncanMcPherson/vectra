using System.Text;
using VectraCompiler.AST.Lexing.Models;
using VectraCompiler.Core;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.AST.Lexing;

/// <summary>
/// This class parses a raw text representation of a VEC file.
/// </summary>
public class Lexer
{
    
    private List<Token>? _tokens;
    private string? _source;
    private int _position;
    private int _line;
    private int _column;
    private int _offset;
    
    private DiagnosticBag _diagnostics = new();

    /// <summary>
    /// Living list of Keywords that are supported
    /// </summary>
    private static readonly HashSet<string> Keywords =
    [
        "space",
        "class",
        "void",
        "return",
        "let",
        "this",
        "number",
        "string",
        "bool",
        "get",
        "set",
        "new",
        "true",
        "false",
        "if",
        "else",
        "while",
        "for", // Consider adding "in" and "foreach"
        // "break",
        // "continue",
        "attempt",
        "recover",
        "debrief",
        "abort",
        "enter"
    ];

    /// <summary>
    /// Living list of currently supported multi-char operators
    /// </summary>
    private static readonly HashSet<string> MultiCharOperators = [
        "!=",
        "==",
        ">=",
        "<=",
        "+=",
        "-=",
        "*=",
        "/="
    ];

    /// <summary>
    /// Living List of currently supported single char operators
    /// </summary>
    private static readonly HashSet<string> SingleCharOperators =
    [
        "+", "-", "*", "/", "=", "<", ">", "!", "%"
    ];

    private static readonly HashSet<string> CollectionOperators =
    [
        "[", "]", "(", ")"
    ];

    /// <summary>
    /// Takes a raw file string and parses it into a list of tokens to be used by the AST generator
    /// </summary>
    /// <param name="source">Raw text of a VEC file</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="Token"/></returns>
    public Result<List<Token>> ReadTokens(string source)
    {
        EnsureClean();
        _source = source;

        while (!IsAtEnd())
        {
            var token = GetToken();
            if (token != null)
                _tokens!.Add(token);
        }
        var endPos = GetCurrentPosition();
        _tokens!.Add(new(TokenType.EndOfFile, "\0", new(endPos, endPos)));
        return _diagnostics.HasErrors ? Result<List<Token>>.Fail(_diagnostics) : Result<List<Token>>.Pass(_tokens, _diagnostics);
    }

    /// <summary>
    /// Retrieves the next token in the file
    /// </summary>
    /// <returns>A <see cref="Token"/></returns>
    private Token? GetToken()
    {
        var (token, isTrivia) = ReadTrivia();
        if (token != null)
            return token;

        if (IsAtEnd())
            return null;
        var startPos = GetCurrentPosition();
        var c = Advance();

        if (char.IsLetter(c) || c == '_')
            return ReadIdentOrKeyword(startPos);
        if (char.IsDigit(c))
            return ReadNumber(startPos);
        return c == '"' ? ReadString(startPos) : ReadSymbolOrOperator(c, startPos);
    }

    private (Token? token, bool isTrivia) ReadTrivia()
    {
        var startPos = GetCurrentPosition();
        var c = Peek();
        if (char.IsWhiteSpace(c))
        {
            while (char.IsWhiteSpace(Peek()))
            {
                Advance();
            }
            var lexeme = _source![startPos.Offset.._offset];
            return (new Token(TokenType.Whitespace, lexeme, new SourceSpan(startPos, GetCurrentPosition())), true);
        }
        
        if (c == '/' && PeekNext() == '/')
        {
            while (Peek() != '\n' && !IsAtEnd())
            {
                Advance();
            }
            var lexeme = _source![startPos.Offset.._offset];
            return (new Token(TokenType.Comment, lexeme, new SourceSpan(startPos, GetCurrentPosition())), true);
        }

        return (null, false);
    }

    /// <summary>
    /// Reads a token as either a keyword or an identifier
    /// </summary>
    /// <param name="start">The position in the file at the start of this token</param>
    /// <param name="line">The line at which this token starts</param>
    /// <param name="column">The column within the line at which this token starts</param>
    /// <returns>A <see cref="Token"/> with type <see cref="TokenType"/>.Identifier or <see cref="TokenType"/>.Keyword</returns>
    private Token ReadIdentOrKeyword(TokenPosition start)
    {
        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
        {
            Advance();
        }

        var lexeme = _source![start.Offset.._offset];
        var type = IsKeyword(lexeme) ? TokenType.Keyword : TokenType.Identifier;
        return new(type, lexeme, new(start, GetCurrentPosition()));
    }

    /// <summary>
    /// Reads a token as a number.
    ///
    /// <remarks>This will only read one decimal, number-like symbols (IP Addresses) should be entered as strings</remarks>
    /// </summary>
    /// <param name="start">The starting position of the token</param>
    /// <returns>A token with type <see cref="TokenType"/>.Number</returns>
    private Token ReadNumber(TokenPosition start)
    {
        var hasSeenDecimal = false;
        while (!IsAtEnd() && (char.IsDigit(Peek()) || Peek() == '.'))
        {
            if (Peek() == '.' && hasSeenDecimal)
            {
                break;
            }

            Advance();
        }

        var lexeme = _source![start.Offset.._offset];
        return new(TokenType.Number, lexeme, new(start, GetCurrentPosition()));
    }

    /// <summary>
    /// Reads a token as a string or text literal
    /// </summary>
    /// <param name="start">The starting position of the token</param>
    /// <returns>A token with type <see cref="TokenType"/>.String</returns>
    /// <exception cref="Exception">Throws an exception if the end of the file is reached before seeing an unescaped double quote</exception>
    private Token ReadString(TokenPosition start)
    {
        var builder = new StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
        {
            var c = Advance();
            if (c == '\\' && !IsAtEnd())
            {
                var next = Advance();
                builder.Append(next switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '"' => '"',
                    '\\' => '\\',
                    _ => next
                });
            }
            else
            {
                builder.Append(c);
            }
            
            // TODO: add error for unterminated strings
        }

        if (IsAtEnd())
        {
            _diagnostics.Error(ErrorCode.UnterminatedStringLiteral, $"Unterminated string at line {start.Line}, column {start.Column}", line: start.Line, column: start.Column);
            return new(TokenType.String, builder.ToString(), new(start, GetCurrentPosition()));
        }

        Advance();
        return new(TokenType.String, builder.ToString(), new(start, GetCurrentPosition()));
    }

    /// <summary>
    /// Reads a token as a symbol or an operator
    /// </summary>
    /// <param name="c">The first character of the operator or symbol.</param>
    /// <param name="start">The starting position of the token</param>
    /// <returns>A token with type <see cref="TokenType"/>.Symbol or <see cref="TokenType"/>.Operator</returns>
    private Token ReadSymbolOrOperator(char c, TokenPosition start)
    {
        var lexeme = c.ToString();
        var next = Peek();
        var twoChar = lexeme + next;
        if (MultiCharOperators.Contains(twoChar))
        {
            Advance();
            lexeme = twoChar;
        }

        var type = IsOperator(lexeme) ? TokenType.Operator : TokenType.Symbol;
        return new(type, lexeme, new(start, GetCurrentPosition()));
    }

    /// <summary>
    /// Resets state to the beginning for the event of lexer reuse
    /// </summary>
    private void EnsureClean()
    {
        if (_tokens == null || _tokens.Count > 0)
            _tokens = new();
        _position = 0;
        _line = 1;
        _column = 1;
        _offset = 0;
    }
    
    #region Utilities
    private TokenPosition GetCurrentPosition() => new(_line, _column, _offset);

    /// <summary>
    /// Checks if our current position is at the end of the file
    /// </summary>
    /// <returns><c>true</c> if the end of the file has been reached, <c>false</c> otherwise</returns>
    private bool IsAtEnd() => _position >= _source!.Length;
    /// <summary>
    /// Gets the next token without advancing our position in the file
    /// </summary>
    /// <returns>The next character in the file, or an EOF marker if we are at the end of the file</returns>
    private char Peek() => IsAtEnd() ? '\0' : _source![_position];
    /// <summary>
    /// Gets the char one position after our current position without advancing our position within the file
    /// </summary>
    /// <returns>The character after the next one</returns>
    private char PeekNext() => _position + 1 >= _source!.Length ? '\0' : _source[_position + 1];
    /// <summary>
    /// Gets the current character and advances our position within the file
    /// </summary>
    /// <returns>The next character in the file</returns>
    private char Advance()
    {
        var current = _source![_position];
        if (current == '\n')
        {
            _line++;
            _column = 1;
        }
        else if (current != '\r')
        {
            _column++;
        }

        if (current == '\t')
        {
            _column += 3;
        }
        _position++;
        _offset++;
        return current;
    }

    /// <summary>
    /// Checks if the provided string matches either a single-char or a multi-char operator
    /// </summary>
    /// <param name="op">The string to compare</param>
    /// <returns><c>true</c> if the string matches a defined operator, <c>false</c> otherwise</returns>
    private static bool IsOperator(string op) => MultiCharOperators.Contains(op) || SingleCharOperators.Contains(op) || CollectionOperators.Contains(op);

    /// <summary>
    /// Checks if the provided string matches a keyword (case-sensitive)
    /// </summary>
    /// <param name="value">The string to compare</param>
    /// <returns><c>true</c> if the string matches a reserved keyword, <c>false</c> otherwise</returns>
    private static bool IsKeyword(string value) => Keywords.Contains(value);

    /// <summary>
    /// Advances our position to skip over comments and whitespaces
    /// </summary>
    private void SkipWhitespaceAndComments()
    {
        while (!IsAtEnd())
        {
            var c = Peek();
            if (char.IsWhiteSpace(c))
            {
                Advance();
            } else if (c == '/' && PeekNext() == '/')
            {
                while (Peek() != '\n' && !IsAtEnd())
                {
                    Advance();
                }
            }
            else
            {
                break;
            }
        }
    }
    #endregion
}