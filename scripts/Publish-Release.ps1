[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$artifactsPath = Join-Path $repoRoot 'artifacts'
$outputPath = Join-Path $artifactsPath 'Release'
$stagingPath = Join-Path $artifactsPath ('.publish-' + [guid]::NewGuid().ToString('N'))

# Only replace the generated release directory, never a linked directory.
foreach ($directory in @($artifactsPath, $outputPath)) {
    if (Test-Path -LiteralPath $directory) {
        $item = Get-Item -LiteralPath $directory -Force
        if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Expected a regular directory inside the repository: $directory"
        }
    }
}

try {
    $publishArguments = @(
        'publish', (Join-Path $repoRoot 'src/RacePoker.Wpf/RacePoker.Wpf.csproj'),
        '--configuration', 'Release', '--runtime', 'win-x64', '--self-contained', 'false',
        '-p:PublishSingleFile=true', '-p:DebugType=embedded', '-p:PublishTrimmed=false',
        '--output', $stagingPath
    )
    if ($Version) { $publishArguments += "-p:Version=$Version" }
    & dotnet @publishArguments
    if ($LASTEXITCODE -ne 0) { throw "Publish failed with exit code $LASTEXITCODE." }

    $files = @(Get-ChildItem -LiteralPath $stagingPath -File -Recurse)
    if ($files.Count -ne 1 -or $files[0].Name -ne 'RacePoker.Wpf.exe') {
        throw 'Expected one executable with no loose DLLs, configuration, or resources.'
    }

    # Publish must succeed before replacing the previous build.
    if (Test-Path -LiteralPath $outputPath) { Remove-Item -LiteralPath $outputPath -Recurse -Force }
    Move-Item -LiteralPath $stagingPath -Destination $outputPath
    Write-Host "Release ready: $(Join-Path $outputPath 'RacePoker.Wpf.exe')"
    Write-Host 'Windows x64; requires the .NET 10 Desktop Runtime.'
}
finally {
    if (Test-Path -LiteralPath $stagingPath) { Remove-Item -LiteralPath $stagingPath -Recurse -Force }
}
