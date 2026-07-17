<#+
.SYNOPSIS
Publishes Wolf Todo TUI and links it into a user executable directory.
#>
param(
    [string]$InstallDirectory = $env:WTODO_INSTALL_DIR,
    [string]$LinkDirectory = $env:WTODO_LINK_DIR
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$project = Join-Path $repositoryRoot 'src/WolfTodo.Tui/WolfTodo.Tui.csproj'

if ([string]::IsNullOrWhiteSpace($InstallDirectory)) {
    if ($IsWindows) {
        $InstallDirectory = Join-Path $env:LOCALAPPDATA 'wtodo/tui'
    }
    elseif ($IsMacOS) {
        $InstallDirectory = Join-Path $HOME 'Library/Application Support/wtodo/tui'
    }
    else {
        $dataHome = if ([string]::IsNullOrWhiteSpace($env:XDG_DATA_HOME)) {
            Join-Path $HOME '.local/share'
        }
        else {
            $env:XDG_DATA_HOME
        }

        $InstallDirectory = Join-Path $dataHome 'wtodo/tui'
    }
}

if ([string]::IsNullOrWhiteSpace($LinkDirectory)) {
    $LinkDirectory = if ($IsWindows) {
        Join-Path $HOME 'bin'
    }
    else {
        Join-Path $HOME '.local/bin'
    }
}

$InstallDirectory = [IO.Path]::GetFullPath($InstallDirectory)
$LinkDirectory = [IO.Path]::GetFullPath($LinkDirectory)
New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $LinkDirectory -Force | Out-Null

dotnet publish $project `
    --configuration Release `
    --output $InstallDirectory `
    --self-contained false `
    -p:UseAppHost=true

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$executableName = if ($IsWindows) { 'wtodo-tui.exe' } else { 'wtodo-tui' }
$executable = Join-Path $InstallDirectory $executableName
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw "Published executable was not found at '$executable'."
}

if ($IsWindows) {
    $link = Join-Path $LinkDirectory 'wtodo-tui.cmd'
    $marker = '@rem Wolf Todo generated launcher'
    if ((Test-Path -LiteralPath $link) -and
        (Get-Content -LiteralPath $link -TotalCount 1) -ne $marker) {
        throw "Refusing to replace non-Wolf Todo launcher '$link'."
    }

    Set-Content -LiteralPath $link -Encoding ASCII -Value @(
        $marker,
        "@`"$executable`" %*"
    )
}
else {
    $link = Join-Path $LinkDirectory 'wtodo-tui'
    if (Test-Path -LiteralPath $link) {
        $existing = Get-Item -LiteralPath $link -Force
        if ($existing.LinkType -ne 'SymbolicLink') {
            throw "Refusing to replace non-symbolic-link '$link'."
        }

        Remove-Item -LiteralPath $link -Force
    }

    New-Item -ItemType SymbolicLink -Path $link -Target $executable | Out-Null
}

$pathEntries = $env:PATH -split [IO.Path]::PathSeparator |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    ForEach-Object { [IO.Path]::GetFullPath($_).TrimEnd([IO.Path]::DirectorySeparatorChar) }
$normalizedLinkDirectory = $LinkDirectory.TrimEnd([IO.Path]::DirectorySeparatorChar)
$comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
$isOnPath = $pathEntries | Where-Object {
    [string]::Equals($_, $normalizedLinkDirectory, $comparison)
}

Write-Host "Installed Wolf Todo TUI to: $InstallDirectory"
Write-Host "Launcher: $link"
if (-not $isOnPath) {
    Write-Warning "'$LinkDirectory' is not on PATH. Add it to your shell configuration."
}
