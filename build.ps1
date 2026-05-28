$ErrorActionPreference = "Stop"

$ProjectDir = "TomatoRadar"
$PublishDir = "$ProjectDir\bin\Release\net6.0-windows\publish"
$InstallerScript = "installer.nsi"
$NSIS = "C:\Program Files (x86)\NSIS\makensis.exe"
$ArtifactsDir = "artifacts"
$DownloadBaseUrl = "https://dl.localizedkorabli.org/tomatoradar/app"

Write-Host "==> Cleaning..." -ForegroundColor Cyan
Remove-Item -Recurse -Force "$ProjectDir\obj", "$ProjectDir\bin" -ErrorAction SilentlyContinue

Write-Host "==> Publishing..." -ForegroundColor Cyan
dotnet publish "$ProjectDir\TomatoRadar.csproj" -c Release
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

$dllPath = Join-Path $PublishDir "TomatoRadar.dll"
$asm = [System.Reflection.AssemblyName]::GetAssemblyName($dllPath)
$version = "$($asm.Version.Major).$($asm.Version.Minor).$($asm.Version.Build)"

$build = (Get-Date).ToString("yyMMddHHmmss")

Write-Host "==> Version: $version  Build: $build" -ForegroundColor Cyan

Write-Host "==> Syncing version into Settings.settings..." -ForegroundColor Cyan
$settingsPath = Join-Path $ProjectDir "Properties\Settings.settings"
$xml = [xml](Get-Content $settingsPath -Encoding UTF8)
$ns = New-Object Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("ns", "http://schemas.microsoft.com/VisualStudio/2004/01/settings")
$xml.SelectSingleNode("//ns:Setting[@Name='SoftwareVersion']/ns:Value", $ns).InnerText = $version
$xml.SelectSingleNode("//ns:Setting[@Name='SoftwareDate']/ns:Value", $ns).InnerText = $build
$xml.Save($settingsPath)
Write-Host "  Updated: $settingsPath"

Write-Host "==> Syncing version into App.config and published config..." -ForegroundColor Cyan
$appConfigPath = Join-Path $ProjectDir "App.config"
$publishedConfigPath = Join-Path $PublishDir "TomatoRadar.dll.config"

foreach ($file in @($appConfigPath, $publishedConfigPath)) {
    $xml = [xml](Get-Content $file -Encoding UTF8)
    $xml.SelectSingleNode("//applicationSettings/TomatoRadar.Properties.Settings/setting[@name='SoftwareVersion']/value").InnerText = $version
    $xml.SelectSingleNode("//applicationSettings/TomatoRadar.Properties.Settings/setting[@name='SoftwareDate']/value").InnerText = $build
    $xml.Save($file)
    Write-Host "  Updated: $file"
}

Write-Host "==> Syncing Settings.Designer.cs default value..." -ForegroundColor Cyan
$designerPath = Join-Path $ProjectDir "Properties\Settings.Designer.cs"
$lines = Get-Content $designerPath -Encoding UTF8
$newLines = @()
$swFound = $false
foreach ($line in $lines) {
    if ($line -match 'DefaultSettingValueAttribute\("') {
        if ($swFound) {
            $line = $line -replace '"[^"]*"', "`"$build`""
        } else {
            $line = $line -replace '"[^"]*"', "`"$version`""
            $swFound = $true
        }
    }
    $newLines += $line
}
Set-Content -Path $designerPath -Value $newLines -Encoding UTF8
Write-Host "  Updated: $designerPath"

if (-not (Test-Path $ArtifactsDir)) {
    New-Item -ItemType Directory -Path $ArtifactsDir | Out-Null
}

Write-Host "==> Building installer..." -ForegroundColor Cyan
& $NSIS "/DPRODUCT_VERSION=$version" "/DPRODUCT_BUILD=$build" $InstallerScript
if ($LASTEXITCODE -ne 0) { throw "NSIS failed" }

$installerFile = Get-ChildItem $ArtifactsDir -Filter "*_Setup.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $installerFile) { throw "Installer not found" }

Write-Host "==> Computing SHA256..." -ForegroundColor Cyan
$sha256 = (Get-FileHash -Path $installerFile.FullName -Algorithm SHA256).Hash.ToUpper()

$fileName = $installerFile.Name

$metadata = @{
    update_server_enabled = $true
    software_latest_version = $version
    software_latest_date = $build
    software_latest_url = "$DownloadBaseUrl/$fileName"
    software_latest_sha256 = $sha256
    shiplist_metadata = @{
        wg = "https://dl.localizedkorabli.org/tomatoradar/ships/wg.json"
        lesta = "https://dl.localizedkorabli.org/tomatoradar/ships/lesta.json"
    }
}

$metadataPath = Join-Path $ArtifactsDir "metadata.json"
$metadata | ConvertTo-Json -Depth 3 | Set-Content -Path $metadataPath -Encoding UTF8

Write-Host "==> Done: $($installerFile.FullName)" -ForegroundColor Green
Write-Host "==> Metadata: $metadataPath" -ForegroundColor Green
