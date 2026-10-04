using System.Reflection;
using MathEngine.Ast;

namespace MathEngine.Evaluation;

/// <summary>
/// Everything the engine needs to know about a built-in function.
/// </summary>
/// <param name="Evaluate">Used by the tree-walking interpreter.</param>
/// <param name="CrlMethod">Used by the compiler to emit a direct call.</param>
/// <param name="PartialDerivative">
/// Given the call's arguments and an argument index, returns the partial
/// dervative with respect to that argument. The chain rule is applied by
/// the differentiator, not here.
/// </param>
public sealed record FunctionDefinition(
    string Name,
    int Arity,
    Func<double[], double> Evaluate,
    MethodInfo? CrlMethod,
    Func<IReadOnlyList<Expr>, int, Expr> PartialDerivative);
