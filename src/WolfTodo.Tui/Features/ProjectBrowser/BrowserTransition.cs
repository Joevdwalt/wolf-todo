using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record BrowserTransition(
    BrowserState State,
    BrowserOperation Operation = BrowserOperation.None,
    string? ProjectPath = null,
    TodoIdentity? TodoIdentity = null,
    TodoTaskUpdate? Update = null,
    ImmutableArray<TodoIdentity> TodoIdentities = default,
    TodoBulkUpdate? BulkUpdate = null);
