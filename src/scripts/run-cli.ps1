<#
.SYNOPSIS
Runs the Wolf Todo command-line interface.
#>
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$project = Join-Path $repositoryRoot 'src/WolfTodo.Cli/WolfTodo.Cli.csproj'

Push-Location $repositoryRoot

try {
    dotnet run --project $project

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
