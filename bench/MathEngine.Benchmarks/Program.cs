using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MathEngine.Ast;
using MathEngine.Compilation;
using MathEngine.Evaluation;

BenchmarkRunner.Run<EvaluationBenchmarks>();

[MemoryDiagnoser]
public class EvaluationBenchmarks
{

    private const string Source = "sin(x) * exp(-x^2) / (1 + x^2)";

    private readonly FunctionRegistry _functions = FunctionRegistry.CreateDefault();
    private Interpreter _interpreter = null!;
    private Expr _expr = null!;
    private Dictionary<string, double> _variables = null!;

    private Func<double, double> _compiled = null!;

    [GlobalSetup]
    public void Setup()
    {
        _expr = Parser.Parse(Source);
        _interpreter = new Interpreter(_functions);
        _variables = new Dictionary<string, double> { ["x"] = 1.25 };
        _compiled = ExpressionCompiler.CompileUnary(_expr, "x", _functions);
    }

    [Benchmark(Baseline = true)]
    public double Interpreted()
    {
        return _interpreter.Evaluate(_expr, _variables);
    }

    [Benchmark]
    public double Compiled()
    {
        return _compiled(1.25);
    }

    [Benchmark]
    public double Native()
    {
        return Math.Sin(1.25) * Math.Exp(-(1.25 * 1.25)) / (1 + 1.25 * 1.25);
    }
}