namespace Wolf.Controls.SelectList;

/// <summary>An option displayed by a select list.</summary>
public sealed record SelectOption(string Label, string? Detail = null, bool IsEnabled = true);
