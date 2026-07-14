using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class ApplicationInputRouter
{
    public ApplicationInputRoute Route(bool featureCapturesInput, ConsoleKeyInfo key, TuiKeyBindings bindings)
    {
        if (featureCapturesInput)
        {
            return ApplicationInputRoute.ActiveFeature;
        }

        if (bindings.MatchesTabPrevious(key))
        {
            return ApplicationInputRoute.PreviousTab;
        }

        return bindings.MatchesTabNext(key)
            ? ApplicationInputRoute.NextTab
            : ApplicationInputRoute.ActiveFeature;
    }
}
