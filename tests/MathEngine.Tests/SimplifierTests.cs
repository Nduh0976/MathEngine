using MathEngine.Ast;
using MathEngine.Calculus;
using MathEngine.Evaluation;
using MathEngine.Printing;
using MathEngine.Rewriting;

namespace MathEngine.Tests
{
    public class SimplifierTests
    {
        private static readonly FunctionRegistry Functions = FunctionRegistry.CreateDefault();

        private static string Derive(string source, string variable = "x")
            => InfixPrinter.Print(
                Simplifier.Simplify(
                    Differentiator.Differentiate(Parser.Parse(source), variable, Functions)));

        [Theory]
        [InlineData("x^2", "2 * x")]
        [InlineData("3*x + 5", "3")]
        [InlineData("x^3", "3 * x^2")]
        [InlineData("sin(x)", "cos(x)")]
        [InlineData("cos(x)", "-sin(x)")]
        [InlineData("exp(x)", "exp(x)")]
        public void Produce_readable_derivatives(string input, string expected)
            => Assert.Equal(expected, Derive(input));

        [Theory]
        [InlineData("x + 0")]
        [InlineData("1 * x")]
        [InlineData("x^1")]
        [InlineData("x / 1")]
        [InlineData("0 + x * 1")]
        public void Collapses_identities_to_the_variable(string input)
            => Assert.Equal(E.Var("x"), Simplifier.Simplify(Parser.Parse(input)));

        [Theory]
        [InlineData("x^2 + 3*x")]
        [InlineData("sin(x) / (1 + x^2)")]
        [InlineData("sqrt(x) * exp(-x)")]
        public void Is_idempotent(string input)
        {
            var once = Simplifier.Simplify(Parser.Parse(input));
            var twice = Simplifier.Simplify(once);
            Assert.Equal(once, twice);
        }

        [Theory]
        [InlineData("a - (b - c)")]
        [InlineData("2 ^ 3 ^ 2")]
        [InlineData("(a + b) * c")]
        [InlineData("a / (b * c)")]
        [InlineData("-x^2")]
        public void Printing_round_trips_through_the_parser(string source)
        {
            var expr = Parser.Parse(source);
            var printed = InfixPrinter.Print(expr);
            var reparsed = Parser.Parse(printed);
            Assert.Equal(expr, reparsed);
        }
    }
}
