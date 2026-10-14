param(
  [string]$GamePath = 'E:\SteamLibrary\steamapps\common\PEAK',
  [string]$DotnetPath = 'dotnet',
  [string]$OutputPath = (Join-Path $PSScriptRoot 'MinecraftFoods-1.0.0-Thunderstore.zip')
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Build.ps1') -GamePath $GamePath -DotnetPath $DotnetPath
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$stage = Join-Path $PSScriptRoot '.package-staging'
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path (Join-Path $stage 'plugins\MinecraftFoods') | Out-Null
foreach ($file in 'manifest.json','README.md','CHANGELOG.md','LICENSE','THIRD_PARTY_NOTICES.md') { Copy-Item (Join-Path $PSScriptRoot $file) $stage -Force }
Copy-Item (Join-Path $PSScriptRoot 'docs\icon.png') (Join-Path $stage 'icon.png') -Force
Copy-Item (Join-Path $PSScriptRoot 'src\bin\Release\netstandard2.1\MinecraftFoods.dll') (Join-Path $stage 'plugins\MinecraftFoods\MinecraftFoods.dll') -Force
Copy-Item (Join-Path $PSScriptRoot 'assets\catalogue.json') (Join-Path $stage 'plugins\MinecraftFoods\catalogue.json') -Force
Copy-Item (Join-Path $PSScriptRoot 'assets\textures') (Join-Path $stage 'plugins\MinecraftFoods\textures') -Recurse -Force
Copy-Item (Join-Path $PSScriptRoot 'assets\sounds') (Join-Path $stage 'plugins\MinecraftFoods\sounds') -Recurse -Force
if (Test-Path -LiteralPath $OutputPath) { Remove-Item -LiteralPath $OutputPath -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $OutputPath -CompressionLevel Optimal
Write-Host "Created $OutputPath"

