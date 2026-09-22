namespace MathEngine.Ast;

/// <summary>Terse constructors for building trees in code.</summary>
public static class E
{
    public static readonly Expr Zero = new Number(0d);
    public static readonly Expr One = new Number(1d);

    public static Expr Num(double value) => new Number(value);
    public static Expr Var(string name) => new Variable(name);

    public static Expr Add(Expr l, Expr r) => new Binary(BinaryOperator.Add, l, r);
    public static Expr Sub(Expr l, Expr r) => new Binary(BinaryOperator.Subtract, l, r);
    public static Expr Mul(Expr l, Expr r) => new Binary(BinaryOperator.Multiply, l, r);
    public static Expr Div(Expr l, Expr r) => new Binary(BinaryOperator.Divide, l, r);
    public static Expr Pow(Expr b, Expr e) => new Binary(BinaryOperator.Power, b, e);
    public static Expr Neg(Expr operand) => new Unary(UnaryOperator.Negate, operand);

    public static Expr Fn(string name, params Expr[] args) =>
        new Call(name, [.. args]);
}
