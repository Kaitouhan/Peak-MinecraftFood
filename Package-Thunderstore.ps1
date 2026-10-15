param(
  [string]$GamePath = 'E:\SteamLibrary\steamapps\common\PEAK',
  [string]$DotnetPath = 'dotnet',
  [string]$OutputPath
)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($OutputPath)) { $OutputPath = Join-Path $PSScriptRoot "MinecraftFoods-$($manifest.version_number)-Thunderstore.zip" }
$stage = Join-Path $PSScriptRoot ".package-staging-build-$($manifest.version_number)"
if (Test-Path -LiteralPath $stage) { throw "Refusing to overwrite existing staging directory: $stage" }
if (Test-Path -LiteralPath $OutputPath) { throw "Refusing to overwrite existing package: $OutputPath" }
& (Join-Path $PSScriptRoot 'Build.ps1') -GamePath $GamePath -DotnetPath $DotnetPath
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
try {
New-Item -ItemType Directory -Force -Path (Join-Path $stage 'plugins\MinecraftFoods') | Out-Null
foreach ($file in 'manifest.json','CHANGELOG.md','LICENSE','THIRD_PARTY_NOTICES.md') { Copy-Item (Join-Path $PSScriptRoot $file) $stage -Force }
Copy-Item (Join-Path $PSScriptRoot 'docs\Thunderstore-README.md') (Join-Path $stage 'README.md') -Force
Copy-Item (Join-Path $PSScriptRoot 'docs\icon.png') (Join-Path $stage 'icon.png') -Force
Copy-Item (Join-Path $PSScriptRoot 'src\bin\Release\netstandard2.1\MinecraftFoods.dll') (Join-Path $stage 'plugins\MinecraftFoods\MinecraftFoods.dll') -Force
Copy-Item (Join-Path $PSScriptRoot 'assets\catalogue.json') (Join-Path $stage 'plugins\MinecraftFoods\catalogue.json') -Force
Copy-Item (Join-Path $PSScriptRoot 'assets\textures') (Join-Path $stage 'plugins\MinecraftFoods\textures') -Recurse -Force
Copy-Item (Join-Path $PSScriptRoot 'assets\soundpack\*') (Join-Path $stage 'plugins\MinecraftFoods') -Recurse -Force
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $OutputPath -CompressionLevel Optimal
Write-Host "Created $OutputPath"
} finally {
  if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
}
