using MathEngine.Syntax;

namespace MathEngine.Tests;

public class LexerTests
{
    private static IReadOnlyList<Token> Lex(string input) => new Lexer(input).Tokenize();

    [Fact]
    public void Tokenizes_arithmetic()
    {
        var types = Lex("2 + 3*x").Select(t => t.Type).ToArray();

        Assert.Equal(
            new[]
            {
                TokenType.Number, TokenType.Plus, TokenType.Number,
                TokenType.Star, TokenType.Identifier, TokenType.End
            },
            types);
    }

    [Theory]
    [InlineData("1", 1d)]
    [InlineData("1.5", 1.5d)]
    [InlineData(".5", 0.5d)]
    [InlineData("1e3", 1000d)]
    [InlineData("1.5e-2", 0.015d)]
    public void Reads_numeric_literals(string input, double expected)
    {
        Assert.Equal(expected, Lex(input)[0].Value);
    }

    [Fact]
    public void Treats_trailing_e_as_identifier()
    {
        var tokens = Lex("2e");

        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal(2d, tokens[0].Value);
        Assert.Equal(TokenType.Identifier, tokens[1].Type);
    }

    [Fact]
    public void Reports_position_of_invalid_character()
    {
        var ex = Assert.Throws<ParseException>(() => Lex("1 + $"));
        Assert.Equal(4, ex.Position);
    }
}
