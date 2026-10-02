using MathEngine.Ast;
using MathEngine.Evaluation;

namespace MathEngine.Calculus
{
    public static class Differentiator
    {
        /// <summary>
        /// Symbolic derivative with respect to <paramref name="=variable"/>.
        /// The result is correct but ugly. Run it thought the Simplifier
        /// </summary>
        public static Expr Differentiate(Expr expr, string variable, FunctionRegistry functions)
        {
            ArgumentNullException.ThrowIfNull(expr);
            ArgumentException.ThrowIfNullOrWhiteSpace(variable);
            ArgumentNullException.ThrowIfNull(functions);

            return Diff(expr, variable, functions);
        }

        private static Expr Diff(Expr expr, string v, FunctionRegistry fns) =>
            expr switch
            {
                Number => E.Zero,

                Variable var => string.Equals(var.Name, v, StringComparison.Ordinal)
                    ? E.One
                    : E.Zero,

                Unary { Operator: UnaryOperator.Negate } u => E.Neg(Diff(u.Operand, v, fns)),

                Binary b => DiffBinary(b, v, fns),

                Call c => DiffCall(c, v, fns),

                _ => throw new NotSupportedException($"Cannot differentiate {expr.GetType().Name}")
            };

         private static Expr DiffBinary(Binary b, string v, FunctionRegistry fns)
        {
            var u = b.Left;
            var w = b.Right;
            var du = Diff(u, v, fns);
            var dw = Diff(w, v, fns);

            return b.Operator switch
            {
                // (u + w)' = u' + w'
                BinaryOperator.Add => E.Add(du, dw),

                // (u - w)' = u' - w'
                BinaryOperator.Subtract => E.Sub(du, dw),

                // (u * w)' = u'w + uw'
                BinaryOperator.Multiply => E.Add(E.Mul(du, w), E.Mul(u, dw)),

                // (u / w)' = (u'w - uw') / w^2
                BinaryOperator.Divide => E.Div(
                    E.Sub(E.Mul(du, w), E.Mul(u, dw)),
                    E.Pow(w, E.Num(2))),

                BinaryOperator.Power => DiffPower(u, w, du, dw),

                _=> throw new NotSupportedException($"Cannot differentiate operator {b.Operator}")
            };
        }

        private static Expr DiffPower(Expr u, Expr w, Expr du, Expr dw)
        {
            // Constant exponent: the power rule. (u^n) = n * u^(n-1) * u'
            if (w is Number n)
            {
                return E.Mul(E.Mul(E.Num(n.Value), E.Pow(u, E.Num(n.Value - 1))),du);
            }

            // General case, via u^w = exp(w * ln u):
            //  (u^w)'= u^w * (w'* ln u + w * u'/u)
            // Valid for u > 0. The special case above avoids needing that
            // restriction for the overwhelmingly common constant-exponent form.
            return E.Mul(
                E.Pow(u, w),
                E.Add(
                    E.Mul(dw, E.Fn("ln", u)),
                    E.Mul(w, E.Div(du, u))));
        }

        private static Expr DiffCall(Call call, string v, FunctionRegistry fns)
        {
            var definition = fns.Get(call.Function);

            // Multivariate chain rule:
            //  d/dv f(a1..an) = sum over i of  (df/dai) * (dai/dv)
            Expr? total = null;

            for (var i = 0; i < call.Arguments.Length; i++)
            {
                var inner = Diff(call.Arguments[i], v, fns);
                var partial = definition.PartialDerivative(call.Arguments, i);
                var term = E.Mul(partial, inner);

                total = total is null ? term : E.Add(total, term);
            }

            return total ?? E.Zero;
        }
    }
}
