param([string]$Output = '', [string]$EnglishSnapshot = '', [string]$Python = (Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'))
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskSource = if (Test-Path -LiteralPath (Join-Path $taskRoot 'Source')) { Join-Path $taskRoot 'Source' } else { $taskRoot }
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $taskCompiler)) { $taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$taskFiles = @('PortableLauncher.cs', 'DeltaSupport.cs', 'RecipeSupport.cs', 'PolishedLauncher.cs', 'FileDiscovery.cs', 'UniversalSupport.cs', 'EnglishSupport.cs', 'EnglishRelease.cs', 'EnglishTests.cs', 'LauncherShell.cs', 'UpgradeSupport.cs', 'UpgradeTests.cs', 'LauncherLayoutTests.cs', 'RuntimeSupport.cs', 'LauncherUpdate.cs') | ForEach-Object { Join-Path $taskSource $_ }
if (!$EnglishSnapshot) {
    & $Python (Join-Path $taskRoot 'Tests/BuildEmbeddedEnglish.py')
    if ($LASTEXITCODE -ne 0) { throw 'English release snapshot failed.' }
    $EnglishSnapshot = Join-Path $taskRoot 'Tests/embedded-english-release.zip'
}
if (!(Test-Path -LiteralPath $EnglishSnapshot)) { throw 'English release snapshot missing.' }
& $taskCompiler /nologo /target:winexe /main:PortableMain /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Core.dll /r:System.Management.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ('/resource:' + $EnglishSnapshot + ',Celesphonia.EnglishRelease.zip') ('/resource:' + (Join-Path $taskRoot 'RuntimePanel.js') + ',Celesphonia.RuntimePanel.js') ('/resource:' + (Join-Path $taskRoot 'RuntimeOfflineLabels.js') + ',Celesphonia.RuntimeOfflineLabels.js') ('/out:' + $(if ($Output) { $Output } else { Join-Path $taskRoot 'Celesphonia Portable Switcher.exe' })) $taskFiles
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
