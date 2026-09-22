using System.Collections.Immutable;

namespace MathEngine.Ast;

/// <summary>
/// Base of the expression tree. Abstract record, so every node gets
/// value equality and a usable ToString for free.
/// </summary>
public abstract record Expr;

public sealed record Number(double Value) : Expr;

public sealed record Variable(string Name) : Expr;

public sealed record Unary(UnaryOperator Operator, Expr Operand) : Expr;

public sealed record Binary(BinaryOperator Operator, Expr Left, Expr Right) : Expr;

public sealed record Call(string Function, ImmutableArray<Expr> Arguments) : Expr
{
    // The synthesized record equality would compare
    // the underlying array reference, so two structurally identical calls would
    // compare unqeual. That would quietly break the simplifier's fixed-point loop,
    // which relies on value equality. So both members are hand-written.
    public bool Equals(Call? other) =>
        other is not null &&
        Function == other.Function &&
        Arguments.SequenceEqual(other.Arguments);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Function);
        foreach (var arg in Arguments)
        {
            hash.Add(arg);
        }
        return hash.ToHashCode();
    }
}
