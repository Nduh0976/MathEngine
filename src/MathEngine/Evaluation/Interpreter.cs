using MathEngine.Ast;

namespace MathEngine.Evaluation;

public sealed class Interpreter
{
    private readonly FunctionRegistry _functions;

    public Interpreter(FunctionRegistry functions) =>
        _functions = functions ?? throw new ArgumentNullException(nameof(functions));

    public double Evaluate(Expr expr, IReadOnlyDictionary<string, double> variables) =>
        expr switch
        {
            Number n => n.Value,

            Variable v => variables.TryGetValue(v.Name, out var value)
                ? value
                : throw new EvaluationException($"Unbound variable '{v.Name}'"),

            Unary { Operator: UnaryOperator.Negate } u => -Evaluate(u.Operand, variables),

            Binary b => ApplyBinary(b.Operator, Evaluate(b.Left, variables), Evaluate(b.Right, variables)),

            Call c => ApplyCall(c, variables),

            _ => throw new EvaluationException($"Unsupported node type {expr.GetType().Name}")
        };

    private static double ApplyBinary(BinaryOperator op, double left, double right) =>
        op switch
        {
            BinaryOperator.Add => left + right,
            BinaryOperator.Subtract => left - right,
            BinaryOperator.Multiply => left * right,
            BinaryOperator.Divide => left / right,
            BinaryOperator.Power => Math.Pow(left, right),
            _ => throw new EvaluationException($"Unsupported node type {op}")
        };

    private double ApplyCall(Call call, IReadOnlyDictionary<string, double> variables)
    {
        var definition = _functions.Get(call.Function);

        if (call.Arguments.Length != definition.Arity)
        {
            throw new EvaluationException($"'{call.Function}' expects {definition.Arity} argument(s) but received {call.Arguments.Length}.");
        }

        var args = new double[call.Arguments.Length];

        for (var i = 0; i < args.Length; i++)
        {
            args[i] = Evaluate(call.Arguments[i], variables);
        }

        return definition.Evaluate(args);
    }
}
