param([string]$Output = '', [string]$EnglishSnapshot = '', [string]$Python = (Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'))
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskSource = if (Test-Path -LiteralPath (Join-Path $taskRoot 'Source')) { Join-Path $taskRoot 'Source' } else { $taskRoot }
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $taskCompiler)) { $taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$taskFiles = @('PortableLauncher.cs', 'RarSupport.cs', 'ExtendedSafeCleanup.cs', 'DeltaSupport.cs', 'RecipeSupport.cs', 'PolishedLauncher.cs', 'FileDiscovery.cs', 'UniversalSupport.cs', 'EnglishSupport.cs', 'EnglishRelease.cs', 'EnglishTests.cs', 'LauncherShell.cs', 'UpgradeSupport.cs', 'UpgradeTests.cs', 'MixedSourceRecovery.cs', 'InstallationRemoval.cs', 'LauncherLayoutTests.cs', 'RuntimeSupport.cs', 'LauncherUpdate.cs') | ForEach-Object { Join-Path $taskSource $_ }
& $Python (Join-Path $taskRoot 'Tests/CompactCatalogs.py')
if ($LASTEXITCODE -ne 0) { throw 'Lossless catalog compression failed.' }
if (!$EnglishSnapshot) {
    & $Python (Join-Path $taskRoot 'Tests/BuildEmbeddedEnglish.py')
    if ($LASTEXITCODE -ne 0) { throw 'English release snapshot failed.' }
    $EnglishSnapshot = Join-Path $taskRoot 'Tests/embedded-english-release.zip'
}
if (!(Test-Path -LiteralPath $EnglishSnapshot)) { throw 'English release snapshot missing.' }
& $Python (Join-Path $taskRoot 'Tests/BuildEmbeddedNativeReferences.py')
if ($LASTEXITCODE -ne 0) { throw 'Native inventory compression failed.' }
$taskNativeReferences = @(Get-ChildItem -LiteralPath $taskRoot -Filter 'NativeReferenceFiles*.json' -File | ForEach-Object {
    $taskSuffix = if ($_.BaseName -eq 'NativeReferenceFiles') { '' } else { '.' + $_.BaseName.Substring('NativeReferenceFiles-'.Length) }
    '/resource:' + (Join-Path $taskRoot ('Staging/native-reference-resources/' + $_.Name + '.gz')) + ',Celesphonia.NativeReferenceFiles' + $taskSuffix + '.json'
})
$taskFiles += $taskNativeReferences
$taskFiles += Join-Path $taskSource 'NotesReader.cs'
$taskFiles += '/resource:' + (Join-Path $taskRoot 'README.md') + ',Celesphonia.QuickStart.md'
$taskFiles += '/resource:' + (Join-Path $taskRoot 'LAUNCHER CHANGELOG.md') + ',Celesphonia.LauncherChangelog.md'
$taskFiles += @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'docs/images') -Filter '*.png' -File | Where-Object { $_.Name -match '^(stat-layout|text-memory|notification|equipment|destination)-' } | ForEach-Object { '/resource:' + $_.FullName + ',Celesphonia.Notes.' + $_.Name })
& $taskCompiler /nologo /target:winexe /main:PortableMain /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Core.dll /r:System.Management.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ('/resource:' + $EnglishSnapshot + ',Celesphonia.EnglishRelease.zip') ('/resource:' + (Join-Path $taskRoot 'RuntimePanel.js') + ',Celesphonia.RuntimePanel.js') ('/resource:' + (Join-Path $taskRoot 'RuntimeOfflineLabels.js') + ',Celesphonia.RuntimeOfflineLabels.js') ('/resource:' + (Join-Path $taskRoot 'StreamRepeatChoiceRescue.js') + ',Celesphonia.StreamRepeatChoiceRescue.js') ('/resource:' + (Join-Path $taskRoot 'GAME MOD CHANGELOG.md') + ',Celesphonia.GameModChangelog.md') ('/resource:' + (Join-Path $taskRoot 'Vendor\SevenZip\7z.exe') + ',Celesphonia.Rar7z.exe') ('/resource:' + (Join-Path $taskRoot 'Vendor\SevenZip\7z.dll') + ',Celesphonia.Rar7z.dll') ('/out:' + $(if ($Output) { $Output } else { Join-Path $taskRoot 'Celesphonia Portable Switcher.exe' })) $taskFiles
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
