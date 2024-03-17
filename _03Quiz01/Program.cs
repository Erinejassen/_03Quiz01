using System;
using System.Collections.Generic;
using System.Linq;

public interface ILexType
{
}

public class LexContainer : ILexType
{
    public List<ILexType> Items { get; }

    public LexContainer()
    {
        Items = new List<ILexType>();
    }
}

public class LexValue : ILexType
{
    public object Value { get; }
    public bool IsExpression { get; }

    public LexValue(object value, bool isExpression)
    {
        Value = value;
        IsExpression = isExpression;
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Sample: ");
        ParseExp("A = D - (C + B)");
        Console.WriteLine(); // Make a new line.

        string[] expressions = {
            "A = (A^2 * (B+C)^2)",
            "B = A + C + D",
            "C = A + D^2",
            "D = (A + B)^2 + C",
            "E = (A - B + C) + (C + D^3)"
        };

        for (int i = 0; i < expressions.Length; i++)
        {
            Console.Write($"{i + 1}: ");
            ParseExp(expressions[i]);
        }
    }

    static void ParseExp(string exp)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(exp))
                return;

            Console.WriteLine($"Sentence: {exp}");
            Console.WriteLine("- <assign> =>");

            var c = Parse(exp.Where(c => !char.IsWhiteSpace(c) && (char.IsLetterOrDigit(c) || IsOperator(c))).ToList());

            Console.WriteLine($"\t| {FormatExpression("<assign>", c, 0)}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[!]: Failed to parse ({ex.Message})\n");
        }
    }

    static string FormatExpression(string expression, LexContainer c, int sub)
    {
        if (c.Items.Count == 0 || string.IsNullOrWhiteSpace(expression))
            return "";

        if (expression.Contains("<assign>"))
            expression = "<id> = <expr>";

        while (c.Items.Count > 0)
        {
            string sb = $"\t| {expression}";

            if (sub > 0)
            {
                sb += new string(')', sub);
                if (sub > 1)
                    sb += $" <cont>{new string(')', sub - 1)}";
            }

            Console.WriteLine(sb);

            var t = c.Items[0];

            if (expression.Contains("<id>"))
            {
                if (!(t is LexValue lexValue) || !lexValue.IsExpression)
                    throw new Exception("Invalid expression found.");

                expression = expression.Replace("<id>", lexValue.Value.ToString());

                c.Items.RemoveAt(0);

                if (c.Items.Count == 0)
                    break;

                t = c.Items[0];

                if (!(t is LexValue nextLexValue) || nextLexValue.IsExpression || !expression.Contains(nextLexValue.Value.ToString()))
                    throw new Exception("Invalid value found.");

                c.Items.RemoveAt(0);
                continue;
            }

            if (expression.Contains("<expr>"))
            {
                if (t is LexValue lexValue)
                {
                    if (!lexValue.IsExpression)
                        throw new Exception($"'{lexValue.Value}' expected.");

                    if (c.Items.Count == 0)
                        throw new Exception("Syntax error.");

                    if (c.Items.Count > 1)
                    {
                        var iop = c.Items[1] as LexValue;
                        if (iop != null && iop.IsExpression)
                            throw new Exception($"'{iop.Value}' expected.");

                        expression = expression.Replace("<expr>", $"<id> {iop.Value} <expr>");
                    }
                    else
                    {
                        expression = expression.Replace("<expr>", "<id>");
                    }
                    continue;
                }

                var container = t as LexContainer;
                c.Items.RemoveAt(0);

                int opIdx = container?.Items.FindIndex(itm => itm is LexValue lv && !lv.IsExpression) ?? -1;

                if (opIdx == -1)
                    throw new Exception("Syntax error.");

                var lv = container.Items[opIdx] as LexValue;
                expression = expression.Replace("<expr>", $"(<id> {lv.Value} <expr>");
                expression = FormatExpression(expression, container, sub + 1);

                if (c.Items.Count > 0)
                {
                    var nextT = c.Items[0];
                    if (!(nextT is LexValue) || ((LexValue)nextT).IsExpression)
                        throw new Exception("Syntax error.");

                    sb = $"{expression} {((LexValue)nextT).Value} <expr>";
                    expression = sb;
                    c.Items.RemoveAt(0);
                    continue;
                }
            }
        }

        if (sub > 0)
            expression += new string(')', sub);
        return expression;
    }

    static LexContainer Parse(List<char> exp)
    {
        LexContainer lexContainer = new LexContainer();
        while (exp.Count > 0)
        {
            if (char.IsLetterOrDigit(exp[0]))
            {
                lexContainer.Items.Add(new LexValue(exp[0], true)); // Expression.
                exp.RemoveAt(0);
                continue;
            }

            if (IsOperator(exp[0]))
            {
                if (exp[0] == '(')
                {
                    int closeParenthesisIdx = GetCloseParenthesisIndex(exp);
                    if (closeParenthesisIdx == -1)
                        throw new Exception("Imbalance parentheses.");

                    List<char> newArray = exp.GetRange(1, closeParenthesisIdx - 1);
                    lexContainer.Items.Add(Parse(newArray));
                    exp.RemoveRange(0, closeParenthesisIdx + 1);
                    continue;
                }

                lexContainer.Items.Add(new LexValue(exp[0], false)); // Operator.
                exp.RemoveAt(0);
            }
        }
        return lexContainer;
    }

    static int GetCloseParenthesisIndex(List<char> c)
    {
        if (c.Count == 0)
            return -1;

        List<char> openParentheses = new List<char>();
        for (int i = 0; i < c.Count; i++)
        {
            if (c[i] == '(')
            {
                openParentheses.Add('(');
                continue;
            }

            if (c[i] == ')')
            {
                if (openParentheses.Count == 0)
                    return -1;

                openParentheses.RemoveAt(openParentheses.Count - 1);
                if (openParentheses.Count == 0)
                    return i;
            }
        }

        return -1;
    }

    static bool IsOperator(char c)
    {
        return "+-*/^=()".Contains(c);
    }
}