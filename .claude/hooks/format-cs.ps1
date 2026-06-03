# Generic ReSharper C# formatter for Claude Code PostToolUse hook.
# Formats ONLY the edited file by targeting its nearest .csproj + --include.
$ErrorActionPreference = 'SilentlyContinue'

$settings = Join-Path $PSScriptRoot 'wrap-lines-override.DotSettings'

$raw = [Console]::In.ReadToEnd()
if ($raw -notmatch '\.cs') { return }  # fast bail: no .cs anywhere in payload
$json = $raw | ConvertFrom-Json
$f = if ($json.tool_response.filePath) { $json.tool_response.filePath } else { $json.tool_input.file_path }
if (-not ($f -and $f -match '\.cs$' -and (Test-Path $f))) { return }

# Walk up from the edited file to the nearest enclosing .csproj.
$dir = Split-Path -Parent (Resolve-Path $f).Path
$target = $null
while ($dir) {
    $proj = Get-ChildItem -Path $dir -Filter *.csproj -File | Select-Object -First 1
    if ($proj) { $target = $proj.FullName; break }
    $parent = Split-Path -Parent $dir
    if ($parent -eq $dir) { break }
    $dir = $parent
}
if (-not $target) { return }

$jb = Join-Path $env:USERPROFILE '.dotnet\tools\jb.exe'
if (-not (Test-Path $jb)) { return }

& $jb cleanupcode `
    '--profile=Built-in: Reformat Code' `
    $target `
    "--include=$f" `
    "--settings=$settings" `
    --no-build 2>$null
dotnet format whitespace --include $f --no-restore $target 2>$null
