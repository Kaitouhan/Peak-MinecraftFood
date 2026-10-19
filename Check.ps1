param([string]$DotnetPath='dotnet')
$ErrorActionPreference='Stop'
$env:DOTNET_CLI_HOME=Join-Path $PSScriptRoot '.build\dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:NUGET_PACKAGES=Join-Path $PSScriptRoot '.build\nuget'
$env:APPDATA=Join-Path $PSScriptRoot '.build\appdata'
New-Item -ItemType Directory -Path $env:APPDATA -Force | Out-Null
$project=Join-Path $PSScriptRoot 'checks\Checks.csproj'
& $DotnetPath restore $project --configfile (Join-Path $PSScriptRoot 'NuGet.Config') -p:NuGetAudit=false
if($LASTEXITCODE -ne 0){throw 'Checks restore failed'}
& $DotnetPath run --project $project --configuration Release --no-restore -- $PSScriptRoot
if($LASTEXITCODE -ne 0){throw 'Checks failed'}
