using MathEngine.Ast;
using MathEngine.Calculus;
using MathEngine.Evaluation;

namespace MathEngine.Tests
{
    public class DifferentiatorTests
    {
        private static readonly FunctionRegistry Functions = FunctionRegistry.CreateDefault();
        private static readonly Interpreter Interpreter = new(Functions);

        [Theory]
        [InlineData("x^2")]
        [InlineData("x^5 - 3*x^2 + 7")]
        [InlineData("1 / x")]
        [InlineData("sin(x)")]
        [InlineData("sin(x) * cos(x)")]
        [InlineData("sin(x^2)")]
        [InlineData("exp(2*x) / (1 + x^2)")]
        [InlineData("sqrt(1 + x^2)")]
        [InlineData("atan(x / 2)")]
        [InlineData("x^x")]
        [InlineData("ln(x) * sqrt(x)")]
        public void Symbolic_derivative_matches_difference(string source)
        {
            var expr = Parser.Parse(source);
            var derivative = Differentiator.Differentiate(expr, "x", Functions);

            // Points chosen positive and away from zero so that ln, sqrt and x^x
            // are all defined. Testing domain edges is a separate concern.
            foreach (var x in new[] {0.3, 0.75, 1.2, 2.5, 4.0 })
            {
                AssertClose(
                    numerical: CentralDifference(expr, x),
                    symbolic: Evaluate(derivative, x),
                    because: $"{source} at x = {x}");
            }
        }

        [Fact]
        public void Derivative_with_respect_to_other_variable_is_zero()
        {
            var derivative = Differentiator.Differentiate(Parser.Parse("y^3"), "x", Functions);
            Assert.Equal(0d, Evaluate(derivative, 2d, y: 5d));
        }

        /// <summary>
        /// Compares a symbolic derivative against a numerical one with a mixed
        /// relative/absolute tolerance.
        /// 
        /// A central difference cannot do better than roughly epsilon^(2/3) relative
        /// error. Truncation error falls as h^2 while roundoff grows as epsilon/h, so
        /// there is a floor no choice of h gets under. 1e-7 leaves several orders
        /// of magnitude of headroom above that floor while still catching a wrong
        /// sign, a missing factor, or a dropped chain-rule term.
        /// 
        /// Note this is NOT Assert.Equal(expected, actual, precision), which rounds
        /// both values to N decimals and compares for equality. Values a few ULPs
        /// apart can land either side of rounding boundary and fail spuriously.
        /// </summary>
        private static void AssertClose(
            double numerical,
            double symbolic,
            string because,
            double tolerance = 1e-7)
        {
            // Dividing by max(1, |value|) gives relative error for large values
            // absolute error near zero, avoiding a blow-up when the derivative is 0.
            var scale = Math.Max(1d, Math.Max(Math.Abs(numerical), Math.Abs(symbolic)));
            var error = Math.Abs(numerical - symbolic) / scale;

            Assert.True(
                error <= tolerance,
                $"""
                 {because}
                  symbolic:  {symbolic:R}
                  numerical: {numerical:R}
                  error:     {error:E3} (tolerance {tolerance:E3})
                """);
        }

        /// <summary>
        /// Central difference, error O(h^2). h = 1e-5 is near optimal: balancing
        /// truncation error (h^2 * f''' / 6) against roundoff (eps * |f| / h)
        /// gives h ~ eps^(1/3) ~ 6e-6. Making h much smaller makes the answer
        /// worse, not better.
        /// </summary>
        private static double CentralDifference(Expr expr, double x, double h = 1e-5)
            => (Evaluate(expr, x + h) - Evaluate(expr, x - h)) / (2 * h);

        private static double Evaluate(Expr expr, double x, double y = 0d)
            => Interpreter.Evaluate(expr, new Dictionary<string, double> { ["x"] = x, ["y"] = y });
    }
}
