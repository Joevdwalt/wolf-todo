using System.Globalization;

namespace WolfTodo.Tui.Features.ProjectBrowser;

/// <summary>
/// Parses the compact, relative date expressions accepted by the task editor.
/// </summary>
public static class DateExpression
{
    public static bool TryParse(string value, DateOnly today, out DateOnly date)
    {
        if (DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date))
        {
            return true;
        }

        var expression = value.Trim().ToLowerInvariant();
        if (expression == "t")
        {
            date = today;
            return true;
        }

        if (expression.Length < 3 || expression[0] is not ('t' or 'w') || expression[1] is not ('+' or '-'))
        {
            date = default;
            return false;
        }

        if (!int.TryParse(expression[2..], NumberStyles.None, CultureInfo.InvariantCulture, out var amount))
        {
            date = default;
            return false;
        }

        try
        {
            var signedAmount = expression[1] == '-' ? -amount : amount;
            date = expression[0] == 't'
                ? today.AddDays(signedAmount)
                : today.AddDays(checked(signedAmount * 7));
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            date = default;
            return false;
        }
        catch (OverflowException)
        {
            date = default;
            return false;
        }
    }
}
