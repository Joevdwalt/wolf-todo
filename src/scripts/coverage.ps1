<#+
.SYNOPSIS
Generates merged Cobertura and HTML coverage reports from Wolf Todo unit tests.
#>
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$coverageRoot = Join-Path $repositoryRoot 'artifacts/coverage'
$rawCoverageRoot = Join-Path $coverageRoot 'raw'
$reportRoot = Join-Path $coverageRoot 'report'
$testProjects = Get-ChildItem -Path (Join-Path $repositoryRoot 'src') -Recurse -Filter '*.Tests.csproj' |
    Where-Object { $_.FullName -notmatch '[\\/]obj[\\/]' }

Push-Location $repositoryRoot

try {
    Remove-Item -Recurse -Force $coverageRoot -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force $rawCoverageRoot | Out-Null
    dotnet tool restore

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    foreach ($project in $testProjects) {
        $projectOutput = Join-Path $rawCoverageRoot $project.BaseName
        dotnet test --project $project.FullName --report-trx --results-directory $projectOutput --coverage --coverage-output-format cobertura

        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }

    dotnet tool run reportgenerator "-reports:$rawCoverageRoot/*/*.cobertura.xml" "-targetdir:$reportRoot" '-reporttypes:Html;Cobertura;TextSummary'

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    Copy-Item (Join-Path $reportRoot 'Cobertura.xml') (Join-Path $coverageRoot 'coverage.cobertura.xml')
    Get-Content (Join-Path $reportRoot 'Summary.txt')
}
finally {
    Pop-Location
}
