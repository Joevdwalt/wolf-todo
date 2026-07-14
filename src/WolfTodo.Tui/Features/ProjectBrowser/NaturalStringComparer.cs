namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed class NaturalStringComparer : IComparer<string>
{
    public static NaturalStringComparer Instance { get; } = new();

    public int Compare(string? left, string? right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left is null)
        {
            return -1;
        }

        if (right is null)
        {
            return 1;
        }

        var leftIndex = 0;
        var rightIndex = 0;

        while (leftIndex < left.Length && rightIndex < right.Length)
        {
            if (char.IsDigit(left[leftIndex]) && char.IsDigit(right[rightIndex]))
            {
                var comparison = CompareNumber(left, ref leftIndex, right, ref rightIndex);
                if (comparison != 0)
                {
                    return comparison;
                }

                continue;
            }

            var characterComparison = char.ToUpperInvariant(left[leftIndex])
                .CompareTo(char.ToUpperInvariant(right[rightIndex]));
            if (characterComparison != 0)
            {
                return characterComparison;
            }

            leftIndex++;
            rightIndex++;
        }

        return (left.Length - leftIndex).CompareTo(right.Length - rightIndex);
    }

    private static int CompareNumber(string left, ref int leftIndex, string right, ref int rightIndex)
    {
        var leftStart = leftIndex;
        var rightStart = rightIndex;

        while (leftIndex < left.Length && char.IsDigit(left[leftIndex]))
        {
            leftIndex++;
        }

        while (rightIndex < right.Length && char.IsDigit(right[rightIndex]))
        {
            rightIndex++;
        }

        var leftSignificant = leftStart;
        var rightSignificant = rightStart;
        while (leftSignificant < leftIndex - 1 && left[leftSignificant] == '0')
        {
            leftSignificant++;
        }

        while (rightSignificant < rightIndex - 1 && right[rightSignificant] == '0')
        {
            rightSignificant++;
        }

        var leftLength = leftIndex - leftSignificant;
        var rightLength = rightIndex - rightSignificant;
        if (leftLength != rightLength)
        {
            return leftLength.CompareTo(rightLength);
        }

        var digitComparison = string.CompareOrdinal(
            left,
            leftSignificant,
            right,
            rightSignificant,
            leftLength);
        if (digitComparison != 0)
        {
            return digitComparison;
        }

        return (leftIndex - leftStart).CompareTo(rightIndex - rightStart);
    }
}
