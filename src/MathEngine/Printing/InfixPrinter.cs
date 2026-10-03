using System.Globalization;
using System.Text;
using MathEngine.Ast;

namespace MathEngine.Printing
{
    public static class InfixPrinter
    {
        public static string Print(Expr expr)
        {
            var builder = new StringBuilder();
            Write(expr, builder, parentPrecedence: 0, isRightOperand: false);
            return builder.ToString();
        }

        private static void Write(Expr expr, StringBuilder builder, int parentPrecedence, bool isRightOperand)
        {
            switch (expr)
            {
                case Number n:
                    builder.Append(n.Value.ToString("R", CultureInfo.InvariantCulture));
                    break;

                case Variable v:
                    builder.Append(v.Name);
                    break;

                case Unary { Operator: UnaryOperator.Negate } u:
                    var needsParentheses = parentPrecedence > 0;
                    if (needsParentheses)
                    {
                        builder.Append('(');
                    }

                    builder.Append('-');
                    Write(u.Operand, builder, 3,isRightOperand: false);
                    break;

                case Binary b:
                    WriteBinary(b, builder, parentPrecedence, isRightOperand);
                    break;

                case Call c:
                    builder.Append(c.Function).Append('(');
                    for (var i = 0; i < c.Arguments.Length; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(", ");
                        }
                        Write(c.Arguments[i], builder, 0, isRightOperand: false);
                    }

                    builder.Append(')');
                    break;

                default:
                    throw new NotSupportedException($"Cannot print {expr.GetType().Name}");
            }
        }

        private static void WriteBinary(Binary b, StringBuilder builder, int parentPrecedence, bool isRightOperand)
        {
            var (symbol, precedence, rightAssociative) = b.Operator switch
            {
                BinaryOperator.Add => (" + ", 1, false),
                BinaryOperator.Subtract => (" - ", 1, false),
                BinaryOperator.Multiply => (" * ", 2, false),
                BinaryOperator.Divide => (" / ", 2, false),
                BinaryOperator.Power => ("^", 3, true),
                _ => throw new NotSupportedException($"Cannot print operator {b.Operator}")
            };

            // Parentheses when the parent binds tighter, or when we sit on the
            // side that associativity does not favor. (a - b) - c needs no parentheses, but a - (b - c) does.
            var needsParentheses = precedence < parentPrecedence || (precedence == parentPrecedence && isRightOperand != rightAssociative);
            if (needsParentheses)
            {
                builder.Append('(');
            }

            Write(b.Left, builder, precedence, isRightOperand: false);
            builder.Append(symbol);
            Write(b.Right, builder, precedence, isRightOperand: true);

            if (needsParentheses)
            {
                builder.Append(')');
            }
        }
    }
}
