using MathEngine.Ast;
using MathEngine.Compilation;
using MathEngine.Evaluation;

namespace MathEngine.Tests
{
    public class CompilerTests
    {
        private static readonly FunctionRegistry Functions = FunctionRegistry.CreateDefault();
        private static readonly Interpreter Interpreter = new(Functions);

        [Theory]
        [InlineData("x^2 + 3*x - 1")]
        [InlineData("sin(x) + cos(x)")]
        [InlineData("exp(-x^2)")]
        [InlineData("sqrt(1 + x^2) / (1 - x)")]
        [InlineData("-x^3")]
        [InlineData("abs(x) + sign(x)")]
        public void Compiled_delegate_matches_interpreter_bit_for_bit(string source)
        {
            var expr = Parser.Parse(source);
            var compiled = ExpressionCompiler.CompileUnary(expr, "x", Functions);

            foreach (var x in new[] { -2.5, -0.5, 0.25, 1.75, 3.0 })
            {
                var interpreted = Interpreter.Evaluate(expr, new Dictionary<string, double> { ["x"] = x });
                var actual = compiled(x);
                Assert.Equal(interpreted, actual);
            }
        }

        [Theory]
        [InlineData("a + b", new[] { "a", "b" })]
        [InlineData("sqrt(a^2 + b^2)", new[] { "a", "b" })]
        [InlineData("a * sin(b) - exp(c)", new[] { "a", "b", "c" })]
        public void Multivariable_compile_matches_interpreter(string source, string[] names)
        {
            var expr = Parser.Parse(source);
            var compiled = ExpressionCompiler.Compile(expr, names, Functions);

            double[][] testCases =
            [
                [0.5, 1.5, 2.0],
                [-1.0, 0.25, 0.75],
                [3.0, -2.0, 1.0]
            ];

            foreach (var args in testCases)
            {
                var bindings = names
                    .Select((n, i) => (n, args[i]))
                    .ToDictionary(p => p.n, p => p.Item2);

                Assert.Equal(Interpreter.Evaluate(expr, bindings), compiled(args.Take(names.Length).ToArray()));
            }
        }

        [Fact]
        public void Constants_are_folded_into_compiled_delegate()
        {
            var expr = Parser.Parse("sin(pi * t)");

            var compiled = ExpressionCompiler.Compile(
                expr,
                ["t"],
                Functions,
                constants: new Dictionary<string, double> { ["pi"] = Math.PI });

                Assert.Equal(1d, compiled([0.5d]), precision: 12);
        }
    }
}
