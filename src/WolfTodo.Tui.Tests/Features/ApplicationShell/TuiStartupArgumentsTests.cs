using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class TuiStartupArgumentsTests
{
    [Fact]
    public void Parse_preserves_codes_for_catalog_validation_and_reports_unsupported_arguments()
    {
        TuiStartupArguments.Parse([]).TaskCode.Should().BeNull();
        TuiStartupArguments.Parse([]).Error.Should().BeNull();
        TuiStartupArguments.Parse(["--open-task", "code"]).TaskCode.Should().Be("code");
        TuiStartupArguments.Parse(["--open-task"]).TaskCode.Should().BeEmpty();
        TuiStartupArguments.Parse(["--unknown"]).Error.Should().Contain("Usage:");
        TuiStartupArguments.Parse(["--open-task", "code", "extra"]).Error.Should().Contain("Usage:");
    }
}
