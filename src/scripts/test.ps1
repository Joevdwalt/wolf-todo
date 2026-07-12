<#+
.SYNOPSIS
Runs all Wolf Todo unit tests and writes TRX test results.
#>
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$resultsRoot = Join-Path $repositoryRoot 'artifacts/test-results'
$testProjects = Get-ChildItem -Path (Join-Path $repositoryRoot 'src') -Recurse -Filter '*.Tests.csproj' |
    Where-Object { $_.FullName -notmatch '[\\/]obj[\\/]' }

Push-Location $repositoryRoot

try {
    foreach ($project in $testProjects) {
        $projectResults = Join-Path $resultsRoot $project.BaseName
        dotnet test --project $project.FullName --report-trx --results-directory $projectResults

        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
}
finally {
    Pop-Location
}
