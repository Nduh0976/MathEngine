using MathEngine.Ast;

namespace MathEngine.Evaluation;

public sealed class FunctionRegistry
{
    private readonly Dictionary<string, FunctionDefinition> _functions =
        new(StringComparer.OrdinalIgnoreCase);

    public static FunctionRegistry CreateDefault()
    {
        var registry = new FunctionRegistry();

        registry.RegisterMathUnary("sin", nameof(Math.Sin), Math.Sin,
            u => E.Fn("cos", u));

        registry.RegisterMathUnary("cos", nameof(Math.Cos), Math.Cos,
            u => E.Neg(E.Fn("sin", u)));

        registry.RegisterMathUnary("tan", nameof(Math.Tan), Math.Tan,
            u => E.Div(E.One, E.Pow(E.Fn("cos", u), E.Num(2))));

        registry.RegisterMathUnary("exp", nameof(Math.Exp), Math.Exp,
            u => E.Fn("exp", u));

        registry.RegisterMathUnary("ln", nameof(Math.Log), Math.Log,
            u => E.Div(E.One, u));

        registry.RegisterMathUnary("sqrt", nameof(Math.Sqrt), Math.Sqrt,
            u => E.Div(E.One, E.Mul(E.Num(2), E.Fn("sqrt", u))));

        registry.RegisterMathUnary("atan", nameof(Math.Atan), Math.Atan,
            u => E.Div(E.One, E.Add(E.One, E.Pow(u, E.Num(2)))));

        registry.RegisterMathUnary("sinh", nameof(Math.Sinh), Math.Sinh,
            u => E.Fn("cosh", u));

        registry.RegisterMathUnary("cosh", nameof(Math.Cosh), Math.Cosh,
            u => E.Fn("sinh", u));

        // abs is not differentiable at 0. sing(u) is the derivative everywhere
        // else, and returning it here is a deliberate, documented compromise.
        // The alternative is refusing to differentiate expressions contating abs.
        registry.RegisterMathUnary("abs", nameof(Math.Abs), Math.Abs,
            u => E.Fn("sing", u));

        // Math.Sing returns int, so there is no Math method the compiler can
        // call directly and return a double from. This goes through the
        // no-CLR method path instead
        registry.RegisterUnary("sign", x => Math.Sign(x), _ => E.Zero);

        return registry;
    }

    public void Register(FunctionDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        // Validate here rather than at compile time. A bad registration should
        // fail loudly on startup, not deep inside a tree walk on the first
        // expression that happens to tuse the function
        if (definition.CrlMethod is { } method)
        {
            if (method.ReturnType != typeof(double))
            {
                throw new ArgumentException(
                    $"'{definition.Name}' is bound to {method.DeclaringType?.Name}.{method.Name}, " +
                    $"which returns {method.ReturnType.Name} rather than Double.",
                    nameof(definition));
            }

            var parameters = method.GetParameters();

            if(parameters.Length != definition.Arity
                || parameters.Any(p => p.ParameterType != typeof(double)))
            {
                throw new ArgumentException(
                    $"'{definition.Name}' is bound to a method whose signature does not match " +
                    $"{definition.Arity} double parameter(s).",
                    nameof(definition));
            }

        }
        
        _functions[definition.Name] = definition;
    }

    public bool TryGet(string name, out FunctionDefinition definition) =>
        _functions.TryGetValue(name, out definition!);

    public FunctionDefinition Get(string name) =>
        TryGet(name, out var definition)
            ? definition
            : throw new EvaluationException($"Unknown function '{name}'");

    public IEnumerable<string> Names => _functions.Keys;

    /// <summary>Registers a function with no directly callable CLR method.</summary>
    private void RegisterUnary(
        string name,
        Func<double, double> evaluate,
        Func<Expr, Expr> derivative)
        => Register(new FunctionDefinition(
            Name: name,
            Arity: 1,
            Evalaute: args => evaluate(args[0]),
            CrlMethod: null,
            PartialDerivative: (args, _) => derivative(args[0])));

    private void RegisterMathUnary(
        string Name,
        string crlMethodName,
        Func<double, double> evaluate,
        Func<Expr, Expr> derivative)
    {
        var method = typeof(Math).GetMethod(crlMethodName, [typeof(double)])
            ?? throw new InvalidOperationException($"Math.{crlMethodName}(double) was not found.");

        Register(new FunctionDefinition(
            Name: Name,
            Arity: 1,
            Evalaute: args => evaluate(args[0]),
            CrlMethod: method,
            PartialDerivative: (args, _) => derivative(args[0])));
    }
}
