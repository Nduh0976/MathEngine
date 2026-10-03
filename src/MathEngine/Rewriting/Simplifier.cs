using System.Collections.Immutable;
using MathEngine.Ast;

namespace MathEngine.Rewriting
{
    public static class Simplifier
    {
        /// <summary>
        /// Applies algebraic rewrite rules until the tree stops changings.
        /// This is not a normal form. It is a readibility pass. Two
        /// mathematically equal exprssions may simplify to different trees.
        /// </summary>
        public static Expr Simplify(Expr expr, int maxIterations = 32)
        {
            ArgumentNullException.ThrowIfNull(expr);

            var current = expr;

            for (var i = 0; i < maxIterations; i++)
            {
                var next = Rewrite(current);

                // Value equality on the records is what makes the fixed-point
                // check a one-liner. This is the payoff for using records.
                if (next == current)
                {
                    return next;
                }

                current = next;
            }

            return current;
        }

        private static Expr Rewrite(Expr expr) =>
            expr switch
            {
                Unary u => SimplifyUnary(u.Operator, Rewrite(u.Operand)),
                Binary b => SimplifyBinary(b.Operator, Rewrite(b.Left), Rewrite(b.Right)),
                Call c => SimplifyCall(c.Function, [..c.Arguments.Select(Rewrite)]),
                _=> expr
            };

        private static Expr SimplifyUnary(UnaryOperator op, Expr operand) =>
            (op, operand) switch
            {
                // -(-u) -> u
                (UnaryOperator.Negate, Unary { Operator: UnaryOperator.Negate } inner) => inner.Operand,

                // -(constant) -> folded constant
                (UnaryOperator.Negate, Number n) => E.Num(-n.Value),

                _ => new Unary(op, operand)
            };

        private static Expr SimplifyBinary(BinaryOperator op, Expr left, Expr right) =>
            op switch
            {
                BinaryOperator.Add => SimplifyAdd(left, right),
                BinaryOperator.Subtract => SimplifySubtract(left, right),
                BinaryOperator.Multiply => SimplifyMultiply(left, right),
                BinaryOperator.Divide => SimplifyDivide(left, right),
                BinaryOperator.Power => SimplifyPower(left, right),
                _ => new Binary(op, left, right)
            };

        private static Expr SimplifyAdd(Expr l, Expr r) =>
            (l, r) switch
            {
                (Number a, Number b) => E.Num(a.Value + b.Value),
                _ when IsZero(l) => r,
                _ when IsZero(r) => l,

                // u + (-w) -> u - w
                (_, Unary { Operator: UnaryOperator.Negate } neg) => SimplifySubtract(l, neg.Operand),

                // u + u -> 2u
                _ when l == r => SimplifyMultiply(E.Num(2), l),

                _ => E.Add(l, r)
            };

        private static Expr SimplifySubtract(Expr l, Expr r) =>
            (l, r) switch
            {
                (Number a, Number b) => E.Num(a.Value - b.Value),
                _ when IsZero(r) => l,
                _ when IsZero(l) => SimplifyUnary(UnaryOperator.Negate, r),
                _ when l == r => E.Zero,

                // u - (-w) -> u + w
                (_, Unary { Operator: UnaryOperator.Negate } neg) => SimplifyAdd(l, neg.Operand),
                _ => E.Sub(l, r)
            };

        private static Expr SimplifyMultiply(Expr l, Expr r) =>
            (l, r) switch
            {
                (Number a, Number b) => E.Num(a.Value * b.Value),

                // Strictly, 0 * NaN is NaN, not 0 - so this rule is not sound
                // over the full IEEE 754 domain. It is kept because expressions
                // that evaluate to NaN are already a bug in the caller's model,
                // and the readability win is large. Documemnt, do not hide.
                _ when IsZero(l) || IsZero(r) => E.Zero,

                _ when IsOne(l) => r,
                _ when IsOne(r) => l,

                _ when IsMinusOne(l) => SimplifyUnary(UnaryOperator.Negate, r),
                _ when IsMinusOne(r) => SimplifyUnary(UnaryOperator.Negate, l),

                // Keep numeric factors on the left: x * 2 -> 2 * x
                (not Number, Number) => E.Mul(r, l),

                // u * u -> u^2
                _ when l == r => E.Pow(l, E.Num(2)),

                _ => E.Mul(l, r)

            };

        private static Expr SimplifyDivide(Expr l, Expr r) =>
            (l, r) switch
            {
                _ when IsOne(r) => l,
                _ when IsZero(l) && !IsZero(r) => E.Zero,
                _ when l == r && !IsZero(r) => E.One,
                (Number a, Number b) when b.Value != 0d => E.Num(a.Value  / b.Value),
                _ => E.Div(l, r)
            };

        private static Expr SimplifyPower(Expr b, Expr e) =>
            (b, e) switch
            {
                _ when IsZero(e) => E.One,
                _ when IsOne(e) => b,
                _ when IsOne(b) => E.One,
                (Number x, Number y) => E.Num(Math.Pow(x.Value, y.Value)),

                // (u^a)^b -> u^(a*b), for numeric a and b
                (Binary { Operator: BinaryOperator.Power, Right: Number inner } p, Number outer)
                    => SimplifyPower(p.Left, E.Num(inner.Value * outer.Value)),

                _ => E.Pow(b, e)
            };

        private static Expr SimplifyCall(string name, ImmutableArray<Expr> args) =>
            new Call(name, args);


        private static bool IsZero(Expr e) => e is Number { Value: 0d };
        private static bool IsOne(Expr e) => e is Number { Value: 1d };
        private static bool IsMinusOne(Expr e) => e is Number { Value: -1d };
    }
}
