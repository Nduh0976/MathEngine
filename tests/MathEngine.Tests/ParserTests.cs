using MathEngine.Ast;
using MathEngine.Syntax;

namespace MathEngine.Tests
{
    public class ParserTests
    {
        [Fact]
        public void Multiplication_bind_tigher_than_addition()
            => Assert.Equal(
                E.Add(E.Num(2), E.Mul(E.Num(3), E.Num(4))),
                Parser.Parse("2 + 3 * 4"));

        [Fact]
        public void Subtraction_is_left_associative()
            => Assert.Equal(
                E.Sub(E.Sub(E.Num(2), E.Num(3)), E.Num(4)),
                Parser.Parse("2 - 3 - 4"));

        [Fact]
        public void Power_is_right_associative()
            => Assert.Equal(
                E.Pow(E.Num(2), E.Pow(E.Num(3), E.Num(4))),
                Parser.Parse("2 ^ 3 ^ 4"));

        [Fact]
        public void Unary_minus_binds_looser_than_power()
            => Assert.Equal(
                E.Neg(E.Pow(E.Var("x"), E.Num(2))),
                Parser.Parse("-x^2"));

        [Fact]
        public void Unary_minus_binds_tigher_than_multiplication()
            => Assert.Equal(
                E.Mul(E.Neg(E.Var("x")), E.Var("y")),
                Parser.Parse("-x * y"));

        [Fact]
        public void Parses_nested_calls()
            => Assert.Equal(
                E.Fn("sin", E.Fn("cos", E.Var("x"))),
                Parser.Parse("sin(cos(x))"));

        [Fact]
        public void Rejects_trailing_garbage()
            => Assert.Throws<ParseException>(() => Parser.Parse("1 + 2)"));
    }
}
