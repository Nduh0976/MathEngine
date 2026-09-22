namespace MathEngine.Syntax;

/// <summary>
/// A lexical token. A readonly record struct: small, copied by vales and
/// compared by value - no allocation per token.
/// </summary>
public readonly record struct Token(TokenType Type, string Text, double Value, int Position)
{
    public override string ToString() => $"{Type}('{Text}')@{Position}";
}
