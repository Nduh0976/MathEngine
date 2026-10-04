using System.Globalization;
using MathEngine.Ast;
using MathEngine.Calculus;
using MathEngine.Evaluation;
using MathEngine.Printing;
using MathEngine.Rewriting;
using MathEngine.Syntax;

var functions = FunctionRegistry.CreateDefault();
var interpreter = new Interpreter(functions);

var variables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
{
    ["pi"] = Math.PI,
    ["e"] = Math.E
};

Console.WriteLine("Expression engine. Commands: :set x 3    :d/dx <expr>    :quit");

while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();

    if (line is null or ":quit")
    {
        break;
    }

    if (string.IsNullOrWhiteSpace(line))
    {
        continue;
    }

    try
    {
        if (line.StartsWith(":set ", StringComparison.Ordinal))
        {
            var parts = line[5..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            variables[parts[0]] = double.Parse(parts[1], CultureInfo.InvariantCulture);

            Console.WriteLine($"{parts[0]} = {variables[parts[0]]}");
            continue;
        }

        if (line.StartsWith(":d/d", StringComparison.Ordinal))
        {
            var variable = line[4..].Split(' ', 2)[0];
            var body = line[(4 + variable.Length)..];
            var derivative = Simplifier.Simplify(
                Differentiator.Differentiate(Parser.Parse(body), variable, functions));

            Console.WriteLine(InfixPrinter.Print(derivative));
            continue;
        }

        var expr = Parser.Parse(line);
        Console.WriteLine(interpreter.Evaluate(expr, variables));
    }
    catch (Exception ex) when (ex is ParseException or EvaluationException)
    {
        Console.WriteLine($"error: {ex.Message}");
    }
}