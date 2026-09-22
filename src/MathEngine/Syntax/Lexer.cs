using System.Globalization;

namespace MathEngine.Syntax;

public sealed class Lexer
{
    private readonly string _source;
    private int _position;

    public Lexer(string source) =>
        _source = source ?? throw new ArgumentNullException(nameof(source));

    public IReadOnlyList<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (true)
        {
            var token = NextToken();
            tokens.Add(token);

            if (token.Type == TokenType.End)
            {
                return tokens;
            }
        }
    }

    private Token NextToken()
    {
        SkipWhiteSpace();

        if (_position >= _source.Length)
        {
            return new Token(TokenType.End, string.Empty, 0d, _position);
        }

        var start = _position;
        var currentChar = _source[_position];

        if (char.IsAsciiDigit(currentChar) || currentChar == '.')
        {
            return ReadNumber(start);
        }

        if (char.IsLetter(currentChar) || currentChar == '_')
        {
            return ReadIdentifier(start);
        }

        _position++;

        return currentChar switch
        {
            '+' => new Token(TokenType.Plus, "+", 0d, start),
            '-' => new Token(TokenType.Minus, "+", 0d, start),
            '*' => new Token(TokenType.Star, "+", 0d, start),
            '/' => new Token(TokenType.Slash, "+", 0d, start),
            '(' => new Token(TokenType.LParen, "+", 0d, start),
            ')' => new Token(TokenType.RParen, "+", 0d, start),
            ',' => new Token(TokenType.Comma, "+", 0d, start),
            _ => throw new ParseException($"Unexpected character '{currentChar}'", start)
        };
    }

    private void SkipWhiteSpace()
    {
        while (_position < _source.Length && char.IsWhiteSpace(_source[_position]))
        {
            _position++;
        }
    }

    private Token ReadNumber(int start)
    {
        while (_position < _source.Length && char.IsAsciiDigit(_source[_position]))
        {
            _position++;
        }

        if (_position < _source.Length && _source[_position] == '.')
        {
            _position++;
            while (_position < _source.Length && char.IsAsciiDigit(_source[_position]))
            {
                _position++;
            }
        }

        TryReadExponent();

        var text = _source[start.._position];

        // InvariantCulture is deliberate. Parsing with the ambient culture means
        // "1.5" silently becomes 15 on a machine with a comma decimal separator.
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new ParseException($"Invalid number '{text}'", start);
        }

        return new Token(TokenType.Number, text, value, start);
    }

    private void TryReadExponent()
    {
        if (_position >= _source.Length || (_source[_position] != 'e' && _source[_position] != 'E'))
        {
            return;
        }

        var save = _position;
        _position++;

        if (_position < _source.Length && (_source[_position] == '+' || _source[_position] == '-'))
        {
            _position++;
        }

        if (_position < _source.Length && char.IsAsciiDigit(_source[_position]))
        {
            while (_position < _source.Length && char.IsAsciiDigit(_source[_position]))
            {
                _position++;
            }
        }
        else
        {
            // Not an exponent after all - "2e" is a number followed by an identifier
            _position = save;
        }
    }

    private Token ReadIdentifier(int start)
    {
        while (_position < _source.Length && (char.IsLetterOrDigit(_source[_position]) || _source[_position] == '_'))
        {
            _position++;
        }

        return new Token(TokenType.Identifier, _source[start.._position], 0d, start);
    }
}
