<#+
.SYNOPSIS
Builds all Wolf Todo production projects.
#>
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$productionProjects = @(
    'src/WolfTodo.Core/WolfTodo.Core.csproj',
    'src/WolfTodo.Tui/WolfTodo.Tui.csproj',
    'src/WolfTodo.Cli/WolfTodo.Cli.csproj'
)

Push-Location $repositoryRoot

try {
    foreach ($project in $productionProjects) {
        dotnet build $project

        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
}
finally {
    Pop-Location
}
