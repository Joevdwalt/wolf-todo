using Wolf.Controls.TextBox;

namespace Wolf.Controls.TextPrompt;

public sealed record TextPromptView(
    TextBoxState Input,
    string Footer,
    string? Context = null,
    bool IsError = false,
    bool DimFooter = false);
