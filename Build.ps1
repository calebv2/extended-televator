param(
    [Parameter(Mandatory = $true)] [string]$GameRoot,
    [string]$Compiler = (Join-Path $env:ProgramFiles 'Unity/Hub/Editor/2020.3.23f1/Editor/Data/Tools/Roslyn/csc.exe'),
    [string]$Output = (Join-Path $PSScriptRoot 'bin')
)

$ErrorActionPreference = 'Stop'
$GameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
$Output = (New-Item -ItemType Directory -Force $Output).FullName
$managed = Join-Path $GameRoot 'A Township Tale_Data/Managed'
$loader = Join-Path $GameRoot 'MelonLoader/net35'
$references = Get-ChildItem -LiteralPath $managed -File -Filter '*.dll' |
    ForEach-Object { '/reference:"' + $_.FullName + '"' }
$references += @('MelonLoader.dll', '0Harmony.dll') |
    ForEach-Object { '/reference:"' + (Join-Path $loader $_) + '"' }
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -File -Filter '*.cs' |
    ForEach-Object { '"' + $_.FullName + '"' }
$dll = Join-Path $Output 'Televator200.dll'
$response = Join-Path $Output 'build.rsp'
@('/nologo', '/target:library', '/langversion:latest', '/nostdlib+', '/optimize+', '/deterministic+',
    ('/out:"' + $dll + '"')) + $references + $sources | Set-Content -LiteralPath $response -Encoding UTF8
& $Compiler ('@' + $response)
if ($LASTEXITCODE -ne 0) { throw 'Extended Televator compilation failed.' }
Get-Item -LiteralPath $dll
