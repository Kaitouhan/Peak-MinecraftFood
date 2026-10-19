param(
  [string]$GamePath = 'E:\SteamLibrary\steamapps\common\PEAK',
  [string]$DotnetPath = 'dotnet',
  [string]$OutputPath
)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$version = $manifest.version_number
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid package version.' }
if ([string]::IsNullOrWhiteSpace($OutputPath)) { $OutputPath = Join-Path $PSScriptRoot "MinecraftFoods-$version-Thunderstore.zip" }
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
$recordPath = $OutputPath + '.release-record.json'
$stage = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".package-staging-build-$version"))
if (!$stage.StartsWith($PSScriptRoot + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Staging path outside repository.' }
foreach ($path in @($stage,$OutputPath,$recordPath)) { if (Test-Path -LiteralPath $path) { throw "Refusing to overwrite: $path" } }
$readmeHash = (Get-FileHash (Join-Path $PSScriptRoot 'README.md')).Hash
foreach ($relative in @('docs\Thunderstore-README.md','docs\THUNDERSTORE_DESCRIPTION.zh-CN.md')) {
  if ((Get-FileHash (Join-Path $PSScriptRoot $relative)).Hash -ne $readmeHash) { throw "Introduction differs: $relative" }
}
$sound = Get-Content (Join-Path $PSScriptRoot 'assets\soundpack\sound_pack.json') -Raw | ConvertFrom-Json
if ($sound.version -ne $version) { throw 'Sound pack version differs from manifest.' }
& (Join-Path $PSScriptRoot 'Build.ps1') -GamePath $GamePath -DotnetPath $DotnetPath
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$dll = Join-Path $PSScriptRoot 'src\bin\Release\netstandard2.1\MinecraftFoods.dll'
if ([Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString(3) -ne $version) { throw 'DLL version differs from manifest.' }
$archiveCreated = $false
try {
  New-Item -ItemType Directory -Path (Join-Path $stage 'plugins\MinecraftFoods') -Force | Out-Null
  foreach ($file in @('manifest.json','CHANGELOG.md','LICENSE','THIRD_PARTY_NOTICES.md')) { Copy-Item (Join-Path $PSScriptRoot $file) $stage }
  Copy-Item (Join-Path $PSScriptRoot 'docs\Thunderstore-README.md') (Join-Path $stage 'README.md')
  Copy-Item (Join-Path $PSScriptRoot 'docs\icon.png') (Join-Path $stage 'icon.png')
  Copy-Item $dll (Join-Path $stage 'plugins\MinecraftFoods\MinecraftFoods.dll')
  Copy-Item (Join-Path $PSScriptRoot 'assets\catalogue.json') (Join-Path $stage 'plugins\MinecraftFoods\catalogue.json')
  Copy-Item (Join-Path $PSScriptRoot 'assets\textures') (Join-Path $stage 'plugins\MinecraftFoods\textures') -Recurse
  Copy-Item (Join-Path $PSScriptRoot 'assets\soundpack\*') (Join-Path $stage 'plugins\MinecraftFoods') -Recurse
  Copy-Item (Join-Path $PSScriptRoot 'docs\settings.example.cfg') (Join-Path $stage 'plugins\MinecraftFoods\settings.example.cfg')
  $inventory = @(Get-ChildItem -LiteralPath $stage -File -Recurse | Sort-Object FullName | ForEach-Object {
    [ordered]@{ path=$_.FullName.Substring($stage.Length+1).Replace('\','/'); sha256=(Get-FileHash -LiteralPath $_.FullName).Hash; bytes=$_.Length }
  })
  New-Item -ItemType Directory -Path (Split-Path $OutputPath -Parent) -Force | Out-Null
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  $zip = [IO.Compression.ZipFile]::Open($OutputPath,[IO.Compression.ZipArchiveMode]::Create)
  $archiveCreated = $true
  try {
    foreach ($entry in $inventory) {
      [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $stage $entry.path),$entry.path,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
  } finally { $zip.Dispose() }
  $sourceFiles = @(Get-ChildItem (Join-Path $PSScriptRoot 'src') -File; Get-ChildItem (Join-Path $PSScriptRoot 'assets') -File -Recurse; Get-Item (Join-Path $PSScriptRoot 'manifest.json'),(Join-Path $PSScriptRoot 'Build.ps1'),(Join-Path $PSScriptRoot 'Package-Thunderstore.ps1'))
  $sourceInventory = @($sourceFiles | Sort-Object FullName | ForEach-Object { [ordered]@{ path=$_.FullName.Substring($PSScriptRoot.Length+1).Replace('\','/'); sha256=(Get-FileHash -LiteralPath $_.FullName).Hash } })
  $commit = (& git -C $PSScriptRoot rev-parse HEAD 2>$null)
  $dirty = @(& git -C $PSScriptRoot status --short --untracked-files=no 2>$null)
  [ordered]@{
    version=$version; builtAtUtc=[DateTime]::UtcNow.ToString('o'); baseCommit=$commit
    trackedChanges=$dirty; note='Local candidate; source hashes include new source files. Gameplay and multiplayer require in-game testing.'
    zipSha256=(Get-FileHash -LiteralPath $OutputPath).Hash; files=$inventory; sources=$sourceInventory
  } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $recordPath -Encoding utf8
  Write-Host "Created $OutputPath"
  Write-Host "Build record $recordPath"
} catch {
  if ($archiveCreated -and (Test-Path -LiteralPath $OutputPath)) { Remove-Item -LiteralPath $OutputPath -Force }
  throw
} finally {
  if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
}
