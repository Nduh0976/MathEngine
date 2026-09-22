namespace MathEngine.Syntax;

public sealed class ParseException : Exception
{
    public ParseException(string message, int position)
        : base($"{message} (at position {position})")
        => Position = position;

    public int Position { get; }
}
