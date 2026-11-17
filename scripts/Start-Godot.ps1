param(
    [string]$GodotPath,
    [switch]$SmokeTest
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $repoRoot 'src/RacePoker.Godot'

if (!$GodotPath) {
    $portable = Join-Path $repoRoot 'artifacts/godot-tools/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64_console.exe'
    if (Test-Path -LiteralPath $portable) { $GodotPath = $portable }
    else {
        $command = Get-Command godot -ErrorAction SilentlyContinue
        if ($command) { $GodotPath = $command.Source }
        else { throw 'Install Godot 4.5.1 .NET, then pass -GodotPath C:\path\to\Godot_console.exe.' }
    }
}
$GodotPath = (Resolve-Path -LiteralPath $GodotPath).Path
$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetPath = if ($dotnetCommand) { $dotnetCommand.Source } else { Join-Path $env:ProgramFiles 'dotnet/dotnet.exe' }

Push-Location $projectPath
try {
    & $dotnetPath build --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Godot C# build failed.' }
    & $GodotPath --headless --editor --import --path $projectPath
    if ($LASTEXITCODE -ne 0) { throw 'Godot asset import failed.' }
    if ($SmokeTest) { & $GodotPath --headless --path $projectPath -- --smoke-test }
    else { & $GodotPath --path $projectPath }
    if ($LASTEXITCODE -ne 0) { throw 'Godot exited with an error.' }
}
finally { Pop-Location }
