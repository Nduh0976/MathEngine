using MathEngine.Syntax;

namespace MathEngine.Ast;

public sealed class Parser
{
    private const int PrecedenceAdditive = 1;
    private const int PrecedenceMultiplicative = 2;
    private const int PrecedencePower = 3;

    private readonly IReadOnlyList<Token> _tokens;
    private int _index;

    private Parser(IReadOnlyList<Token> tokens) => _tokens = tokens;

    public static Expr Parse(string input)
    {
        var tokens = new Lexer(input).Tokenize();
        var parser = new Parser(tokens);
        var expr = parser.ParseExpression(0);
        parser.Expect(TokenType.End);

        return expr;
    }

    private Token Current => _tokens[_index];

    private void Advance() => _index++;

    private Token Expect(TokenType type)
    {
        if (Current.Type != type)
        {
            throw new ParseException($"Expected {type}, but found {Current.Type}", Current.Position);
        }

        var token = Current;
        Advance();

        return token;
    }

    /// <summary>
    /// Precedence climbing: parse a prefix, then keep absorbing binary
    /// operators whose precedence is at least <paramref name="=minPrecedence"/>.
    /// </summary>
    private Expr ParseExpression(int minPrecedence)
    {
        var left = ParseUnary();

        while (TryGetBinaryOperator(Current.Type, out var op, out var precedence, out var rightAssociative ) && precedence >= minPrecedence)
        {
            Advance();

            // Left-associative: the right operand must bind *tighter*, so raise
            // the floor. Right-associative: keep the floor, so equal precedence
            // nests to the right. That single line is all associativity is.
            var nextMinimum = rightAssociative ? precedence : precedence + 1;

            var right = ParseExpression(nextMinimum);
            left = new Binary(op, left, right);
        }

        return left;
    }

    private Expr ParseUnary()
    {
        switch (Current.Type)
        {
            case TokenType.Minus:
                Advance();
                // Bind tighter than '*' but looser than '^', so that
                // -x^2 parses as -(x^2), matching normal notation.
                return E.Neg(ParseExpression(PrecedencePower));

            case TokenType.Plus:
                Advance();
                return ParseUnary();

            default:
                return ParsePrimary();
        }
    }

    private Expr ParsePrimary()
    {
        switch (Current.Type)
        {
            case TokenType.Number:
                var token = Current;
                Advance();
                return E.Num(token.Value);

            case TokenType.Identifier:
                var name = Current.Text;
                Advance();
                return Current.Type == TokenType.LParen
                    ? ParseCall(name)
                    : E.Var(name);

            case TokenType.LParen:
                Advance();
                var inner = ParseExpression(0);
                Expect(TokenType.RParen);
                return inner;

            default:
                throw new ParseException($"Unexpected {Current.Type}", Current.Position);
        }
    }

    private Expr ParseCall(string name)
    {
        Expect(TokenType.LParen);

        var args = new List<Expr>();

        if (Current.Type != TokenType.RParen)
        {
            args.Add(ParseExpression(0));
            while (Current.Type == TokenType.Comma)
            {
                Advance();
                args.Add(ParseExpression(0));
            }
        }

        Expect(TokenType.RParen);

        return E.Fn(name, [.. args]);
    }

    private static bool TryGetBinaryOperator(
        TokenType type,
        out BinaryOperator op,
        out int precedence,
        out bool rightAssociative)
    {
        (op, precedence, rightAssociative) = type switch
        {
            TokenType.Plus => (BinaryOperator.Add, PrecedenceAdditive, false),
            TokenType.Minus => (BinaryOperator.Subtract, PrecedenceAdditive, false),
            TokenType.Star => (BinaryOperator.Multiply, PrecedenceMultiplicative, false),
            TokenType.Slash => (BinaryOperator.Divide, PrecedenceMultiplicative, false),
            TokenType.Caret => (BinaryOperator.Power, PrecedencePower, true),
                _ => (default(BinaryOperator), 0, false)
        };
        
        return precedence > 0;
    }
}

