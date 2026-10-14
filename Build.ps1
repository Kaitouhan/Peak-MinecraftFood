param([string]$GamePath='E:\SteamLibrary\steamapps\common\PEAK', [string]$DotnetPath='C:\Users\Mr\Documents\Codex\2026-10-11\e-steamlibrary-steamapps-common-peak\work\webtablet-build\dotnet-sdk\dotnet.exe')
$ErrorActionPreference='Stop'
$env:DOTNET_CLI_HOME=Join-Path $PSScriptRoot '.build\dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:NUGET_PACKAGES=Join-Path $PSScriptRoot '.build\nuget'
$env:APPDATA=Join-Path $PSScriptRoot '.build\appdata'
New-Item -ItemType Directory -Path $env:APPDATA -Force | Out-Null
$project=Join-Path $PSScriptRoot 'src\MinecraftFoods.csproj'
& $DotnetPath restore $project --configfile (Join-Path $PSScriptRoot 'NuGet.Config') "-p:GameDir=$GamePath" -p:NuGetAudit=false
if($LASTEXITCODE -ne 0){throw 'Restore failed'}
& $DotnetPath build $project -c Release --no-restore "-p:GameDir=$GamePath" -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'Build failed'}

