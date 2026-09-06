namespace Wolf.Controls;

/// <summary>A semantic input event supplied by the host application.</summary>
public sealed record ControlInput(ControlInputKind Kind, char Character = '\0')
{
    public static ControlInput FromCharacter(char character) => new(ControlInputKind.Character, character);
}
