using MathEngine.Ast;
using MathEngine.Evaluation;

using LinqExpr = System.Linq.Expressions.Expression;

namespace MathEngine.Compilation
{
    public class ExpressionCompiler
    {
        /// <summary>
        /// Compiles a single-variable expression into a JIT-compiled delegate.
        /// </summary>
        public static Func<double, double> CompileUnary(
            Expr expr,
            string parameterName,
            FunctionRegistry functions)
        {
            var parameter = LinqExpr.Parameter(typeof(double), parameterName);

            var bindings = new Dictionary<string, LinqExpr>(StringComparer.Ordinal)
            {
                [parameterName] = parameter
            };

            var body = Build(expr, bindings, functions);

            return LinqExpr.Lambda<Func<double, double>>(body, parameter).Compile();
        }

        /// <summary>
        /// Compiles a multi-variable expression into a delegate over an argument
        /// array. Variables not listed are resolved as constants from
        /// <paramref name="constants"/>. Which is how pi and e get folded in.
        /// </summary>
        public static Func<double[], double> Compile(
            Expr expr,
            IReadOnlyList<string> parameterNames,
            FunctionRegistry functions,
            IReadOnlyDictionary<string, double>? constants = null)
        {
            var array = LinqExpr.Parameter(typeof(double[]), "args");

            var bindings = new Dictionary<string, LinqExpr>(StringComparer.Ordinal);

            for (var i = 0; i < parameterNames.Count; i++)
            {
                var name = parameterNames[i];
                bindings[name] = LinqExpr.ArrayIndex(array, LinqExpr.Constant(i));
            }

            if (constants is not null)
            {
                foreach (var (name, value) in constants)
                {
                    bindings.TryAdd(name, LinqExpr.Constant(value));
                }
            }

            var body = Build(expr, bindings, functions);

            return LinqExpr.Lambda<Func<double[], double>>(body, array).Compile();
        }

        private static LinqExpr Build(
            Expr expr,
            IReadOnlyDictionary<string, LinqExpr> bindings,
            FunctionRegistry functions) =>
            expr switch
            {
                Number n => LinqExpr.Constant(n.Value),

                Variable v => bindings.TryGetValue(v.Name, out var bound)
                    ? bound
                    : throw new EvaluationException($"Unbound variable '{v.Name}'"),

                Unary { Operator: UnaryOperator.Negate } u => LinqExpr.Negate(Build(u.Operand, bindings, functions)),

                Binary b => BuildBinary(
                    b.Operator,
                    Build(b.Left, bindings, functions),
                    Build(b.Right, bindings, functions)),

                Call c => BuildCall(c, bindings, functions),

                _ => throw new NotSupportedException($"Cannot compile {expr.GetType().Name}")
            };

        private static LinqExpr BuildBinary(BinaryOperator op, LinqExpr left, LinqExpr right) =>
            op switch
            {
                BinaryOperator.Add => LinqExpr.Add(left, right),
                BinaryOperator.Subtract => LinqExpr.Subtract(left, right),
                BinaryOperator.Multiply => LinqExpr.Multiply(left, right),
                BinaryOperator.Divide => LinqExpr.Divide(left, right),

                // For doubles this resolves to call to Math.Pow(double, double)
                BinaryOperator.Power => LinqExpr.Power(left, right),

                _ => throw new NotSupportedException($"Cannot compile operator '{op}'")
            };

        private static LinqExpr BuildCall(
            Call call,
            IReadOnlyDictionary<string, LinqExpr> bindings,
            FunctionRegistry functions)
        {
            var definition = functions.Get(call.Function);

            if (call.Arguments.Length != definition.Arity)
            {
                throw new EvaluationException($"'{call.Function}' expects {definition.Arity} argument(s), but received {call.Arguments.Length}");
            }

            var arguments = call.Arguments.Select(arg => Build(arg, bindings, functions)).ToArray();

            if (definition.CrlMethod is { } method)
            {
                return LinqExpr.Call(method, arguments);
            }
            
            // No direct CLR method (Math.Sign returns int, for example). Fall back
            // to invoking the registered delegate through a captured constant.
            // still faster than walking the tree, but not a direct static call.
            var target = LinqExpr.Constant(definition.Evaluate);
            var packed = LinqExpr.NewArrayInit(typeof(double), arguments);

            return LinqExpr.Invoke(target, packed);
        }
    }
}
