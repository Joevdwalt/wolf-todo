using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record SavedTodoQuery(string Source, ImmutableArray<SavedTodoQueryTerm> Terms)
{
    private static readonly DateOnly ValidationDate = new(2000, 1, 15);

    public bool Matches(TodoItem todo, string projectTitle, DateOnly today) =>
        Terms.All(term => term.Matches(todo, projectTitle, today));

    public static bool TryParse(string source, out SavedTodoQuery query, out string error)
    {
        var values = source.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (values.Length == 0)
        {
            query = null!;
            error = "query must contain at least one term";
            return false;
        }

        var terms = ImmutableArray.CreateBuilder<SavedTodoQueryTerm>();
        foreach (var value in values)
        {
            var separator = value.IndexOf(':');
            if (separator <= 0 || separator == value.Length - 1)
            {
                query = null!;
                error = $"term '{value}' must use field:value syntax";
                return false;
            }

            var field = value[..separator].ToLowerInvariant();
            var operand = value[(separator + 1)..];
            if (!SavedTodoQueryTerm.TryCreate(field, operand, ValidationDate, out var term, out error))
            {
                query = null!;
                return false;
            }

            terms.Add(term);
        }

        query = new SavedTodoQuery(source.Trim(), terms.ToImmutable());
        error = string.Empty;
        return true;
    }
}

public enum SavedTodoQueryField { Scheduled, Tag, Project, Text, Priority }

public enum SavedTodoDateOperator { Equal, Before, BeforeOrEqual, After, AfterOrEqual }

public sealed record SavedTodoQueryTerm(
    SavedTodoQueryField Field,
    string Value,
    SavedTodoDateOperator DateOperator = SavedTodoDateOperator.Equal)
{
    public bool Matches(TodoItem todo, string projectTitle, DateOnly today) => Field switch
    {
        SavedTodoQueryField.Scheduled => MatchesScheduled(todo, today),
        SavedTodoQueryField.Tag => todo.Tags.Any(tag => string.Equals(
            tag.TrimStart('#'), Value.TrimStart('#'), StringComparison.OrdinalIgnoreCase)),
        SavedTodoQueryField.Project => projectTitle.Contains(Value, StringComparison.OrdinalIgnoreCase),
        SavedTodoQueryField.Priority => MatchesPriority(todo.Priority),
        _ => MatchesText(todo)
    };

    internal static bool TryCreate(
        string field,
        string operand,
        DateOnly validationDate,
        out SavedTodoQueryTerm term,
        out string error)
    {
        if (!Enum.TryParse<SavedTodoQueryField>(field, true, out var parsedField))
        {
            term = null!;
            error = $"field '{field}' is not supported; use scheduled, tag, project, text, or priority";
            return false;
        }

        var dateOperator = SavedTodoDateOperator.Equal;
        if (parsedField == SavedTodoQueryField.Scheduled)
        {
            (dateOperator, operand) = ParseDateOperator(operand);
            if (!DateExpression.TryParse(operand, validationDate, out _))
            {
                term = null!;
                error = $"scheduled value '{operand}' must be an ISO date or relative expression such as t, t-1, or w+1";
                return false;
            }
        }
        else if (parsedField == SavedTodoQueryField.Priority &&
                 !Enum.TryParse<TodoPriority>(operand, true, out _))
        {
            term = null!;
            error = $"priority value '{operand}' must be lowest, low, medium, high, or highest";
            return false;
        }

        term = new SavedTodoQueryTerm(parsedField, operand, dateOperator);
        error = string.Empty;
        return true;
    }

    private bool MatchesScheduled(TodoItem todo, DateOnly today)
    {
        if (todo.Schedule is null || !DateExpression.TryParse(Value, today, out var target))
        {
            return false;
        }

        return DateOperator switch
        {
            SavedTodoDateOperator.Before => todo.Schedule.Date < target,
            SavedTodoDateOperator.BeforeOrEqual => todo.Schedule.Date <= target,
            SavedTodoDateOperator.After => todo.Schedule.Date > target,
            SavedTodoDateOperator.AfterOrEqual => todo.Schedule.Date >= target,
            _ => todo.Schedule.Date == target
        };
    }

    private bool MatchesPriority(TodoPriority? priority) =>
        Enum.TryParse<TodoPriority>(Value, true, out var expected) &&
        (priority ?? TodoPriority.Medium) == expected;

    private bool MatchesText(TodoItem todo) =>
        Contains(todo.Title, Value) || Contains(todo.ExternalReference, Value) ||
        Contains(todo.SectionPath, Value) || todo.Tags.Any(tag => Contains(tag, Value));

    private static bool Contains(string? source, string value) =>
        source?.Contains(value, StringComparison.OrdinalIgnoreCase) == true;

    private static (SavedTodoDateOperator Operator, string Operand) ParseDateOperator(string operand)
    {
        if (operand.StartsWith("<=", StringComparison.Ordinal))
        {
            return (SavedTodoDateOperator.BeforeOrEqual, operand[2..]);
        }

        if (operand.StartsWith(">=", StringComparison.Ordinal))
        {
            return (SavedTodoDateOperator.AfterOrEqual, operand[2..]);
        }

        if (operand.StartsWith('<'))
        {
            return (SavedTodoDateOperator.Before, operand[1..]);
        }

        if (operand.StartsWith('>'))
        {
            return (SavedTodoDateOperator.After, operand[1..]);
        }

        return operand.StartsWith('=')
            ? (SavedTodoDateOperator.Equal, operand[1..])
            : (SavedTodoDateOperator.Equal, operand);
    }
}
