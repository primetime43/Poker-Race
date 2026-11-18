[CmdletBinding()]
param(
    [string]$GodotPath,
    [string]$ExportTemplatesZip,
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repoRoot 'src/RacePoker.Godot'
if (!$GodotPath) {
    $GodotPath = Join-Path $repoRoot 'artifacts/godot-tools/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64_console.exe'
    if (!(Test-Path -LiteralPath $GodotPath)) {
        $command = Get-Command godot -ErrorAction SilentlyContinue
        if (!$command) { throw 'Pass -GodotPath pointing to the Godot 4.5.1 .NET console executable.' }
        $GodotPath = $command.Source
    }
}
$GodotPath = (Resolve-Path -LiteralPath $GodotPath).Path
$engineVersion = & $GodotPath --version
if ($LASTEXITCODE -ne 0 -or $engineVersion -notmatch '^4\.5\.1\.stable\.mono\.') {
    throw 'This export preset requires Godot 4.5.1 .NET and its matching export templates.'
}

# Use templates installed through the editor, or install the Windows x64 subset
# from the official .NET template archive. No downloads happen in this script.
$templatesPath = Join-Path $env:APPDATA 'Godot/export_templates/4.5.1.stable.mono'
if (!$ExportTemplatesZip) {
    $cached = Join-Path $repoRoot 'artifacts/godot-tools/export-templates.tpz'
    if (Test-Path -LiteralPath $cached) { $ExportTemplatesZip = $cached }
}
if ($ExportTemplatesZip) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    New-Item -ItemType Directory -Path $templatesPath -Force | Out-Null
    $archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $ExportTemplatesZip).Path)
    try {
        $versionEntry = $archive.GetEntry('templates/version.txt')
        if (!$versionEntry) { throw 'Missing template version.' }
        $reader = [IO.StreamReader]::new($versionEntry.Open())
        try { if ($reader.ReadToEnd().Trim() -ne '4.5.1.stable.mono') { throw 'Wrong export template version.' } }
        finally { $reader.Dispose() }
        foreach ($name in @('windows_debug_x86_64.exe', 'windows_debug_x86_64_console.exe', 'windows_release_x86_64.exe', 'windows_release_x86_64_console.exe')) {
            $entry = $archive.GetEntry("templates/$name")
            if (!$entry) { throw "Missing template: $name" }
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $templatesPath $name), $true)
        }
    }
    finally { $archive.Dispose() }
}
if (!(Test-Path -LiteralPath (Join-Path $templatesPath 'windows_release_x86_64.exe'))) {
    throw 'Install the Godot 4.5.1 .NET export templates in the editor, or pass -ExportTemplatesZip.'
}

$outputRoot = Join-Path $repoRoot 'artifacts/Godot'
$stage = Join-Path $outputRoot ('.publish-' + [guid]::NewGuid().ToString('N'))
$output = Join-Path $outputRoot 'Windows'
foreach ($path in @((Join-Path $repoRoot 'artifacts'), $outputRoot, $output)) {
    if (Test-Path -LiteralPath $path) {
        $item = Get-Item -LiteralPath $path -Force
        if (!$item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Expected a regular directory: $path" }
    }
}
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetPath = if ($dotnet) { $dotnet.Source } else { Join-Path $env:ProgramFiles 'dotnet/dotnet.exe' }
$env:PATH = "$(Split-Path $dotnetPath -Parent);$env:PATH"
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$versionFiles = @('RacePoker.Godot.csproj', 'project.godot', 'export_presets.cfg')
$originalContents = @{}
Push-Location $projectPath
try {
    if ($Version) {
        foreach ($file in $versionFiles) { $originalContents[$file] = [IO.File]::ReadAllText((Join-Path $projectPath $file)) }
        $resourceVersion = ($Version -split '[-+]')[0] + '.0'
        [IO.File]::WriteAllText((Join-Path $projectPath 'RacePoker.Godot.csproj'), ($originalContents['RacePoker.Godot.csproj'] -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>"))
        [IO.File]::WriteAllText((Join-Path $projectPath 'project.godot'), ($originalContents['project.godot'] -replace '(?m)^config/version="[^"]+"', "config/version=`"$Version`""))
        [IO.File]::WriteAllText((Join-Path $projectPath 'export_presets.cfg'), ($originalContents['export_presets.cfg'] -replace '(?m)^(application/(?:file|product)_version)="[^"]+"', "`$1=`"$resourceVersion`""))
    }
    & $dotnetPath build --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Godot C# build failed.' }
    & $GodotPath --headless --editor --import --path $projectPath
    if ($LASTEXITCODE -ne 0) { throw 'Godot asset import failed.' }
    & $GodotPath --headless --path $projectPath --export-release 'Windows Desktop' (Join-Path $stage 'RacePoker.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Godot Windows export failed.' }
    $runtime = @(Get-ChildItem -LiteralPath $stage -Filter coreclr.dll -Recurse)
    if ($runtime.Count -eq 0 -or !(Test-Path -LiteralPath (Join-Path $stage 'RacePoker.exe'))) {
        throw 'Export did not include the executable and self-contained .NET runtime.'
    }
    $runtimeConfig = Get-Content -LiteralPath (Join-Path $runtime[0].DirectoryName 'RacePoker.Godot.runtimeconfig.json') -Raw | ConvertFrom-Json
    $runtimeVersion = ($runtimeConfig.runtimeOptions.includedFrameworks | Where-Object name -eq 'Microsoft.NETCore.App').version
    $packageCache = $env:NUGET_PACKAGES
    if (!$packageCache) {
        $cacheDescription = & $dotnetPath nuget locals global-packages --list
        if ($LASTEXITCODE -ne 0 -or $cacheDescription -notmatch '^.*?:\s*(.+)$') { throw 'Could not locate runtime license notices.' }
        $packageCache = $Matches[1]
    }
    $runtimePackage = Join-Path $packageCache "microsoft.netcore.app.runtime.win-x64/$runtimeVersion"
    Copy-Item -LiteralPath (Join-Path $runtimePackage 'LICENSE.TXT') -Destination (Join-Path $stage 'DotNet-LICENSE.txt')
    Copy-Item -LiteralPath (Join-Path $runtimePackage 'THIRD-PARTY-NOTICES.TXT') -Destination (Join-Path $stage 'DotNet-THIRD-PARTY-NOTICES.txt')
    $version = ([xml](Get-Content -LiteralPath (Join-Path $projectPath 'RacePoker.Godot.csproj') -Raw)).Project.PropertyGroup.Version
    $readme = [IO.File]::ReadAllText((Join-Path $repoRoot 'docs/Godot-Portable-Readme.txt')) -replace '(?m)^RACE POKER .*? - WINDOWS x64', "RACE POKER $version - WINDOWS x64"
    [IO.File]::WriteAllText((Join-Path $stage 'README.txt'), $readme)
    Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/Godot-LICENSE.txt'), (Join-Path $repoRoot 'docs/Godot-COPYRIGHT.txt') -Destination $stage
    $license = Join-Path $repoRoot 'LICENSE'
    if (Test-Path -LiteralPath $license) { Copy-Item -LiteralPath $license -Destination $stage }
    $zip = Join-Path $outputRoot "RacePoker-$version-Windows-x64.zip"
    $stagedZip = "$stage.zip"
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $stagedZip
    # Both deletion targets are fixed children of the checked repository directory.
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
    Move-Item -LiteralPath $stage -Destination $output
    Move-Item -LiteralPath $stagedZip -Destination $zip -Force
    Get-FileHash -LiteralPath $zip -Algorithm SHA256 | Format-List
    Write-Host "Portable build ready: $zip"
}
finally {
    foreach ($file in $originalContents.Keys) { [IO.File]::WriteAllText((Join-Path $projectPath $file), $originalContents[$file]) }
    Pop-Location
}
