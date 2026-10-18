param(
    [string] $Profiles = '',
    [string] $Results = '',
    [switch] $OnlyLegacy
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (!$Profiles) { $Profiles = Join-Path $root 'profiles.json' }
if (!$Results) { $Results = Join-Path $PSScriptRoot 'launcher-redesign-native-qa' }
$Profiles = (Resolve-Path -LiteralPath $Profiles).Path
if (!(Test-Path -LiteralPath $Results)) { New-Item -ItemType Directory -Path $Results | Out-Null }
$Results = (Resolve-Path -LiteralPath $Results).Path
$temporary = Join-Path $env:TEMP ('Cph-native-layout-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporary | Out-Null
try {
    foreach($name in @('mod-recipes.json','version-fingerprints.json','README.txt')){
        Copy-Item -LiteralPath (Join-Path $root $name) -Destination $temporary
    }
    Copy-Item -LiteralPath $Profiles -Destination (Join-Path $temporary 'profiles.json')
    Copy-Item -LiteralPath $Profiles -Destination (Join-Path $temporary 'real-fixture.json')
    Set-Content -LiteralPath (Join-Path $temporary 'empty-fixture.json') -Encoding UTF8 -Value '{"Profiles":[],"SearchRoots":[],"SuppressedRoots":[],"LauncherTheme":"Midnight","InstallRoot":"C:\\NonexistentCphQAInstallRoot"}'
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $source = @('PortableLauncher.cs','RarSupport.cs','ExtendedSafeCleanup.cs','DeltaSupport.cs',
        'RecipeSupport.cs','PolishedLauncher.cs','FileDiscovery.cs','UniversalSupport.cs',
        'EnglishSupport.cs','EnglishRelease.cs','EnglishTests.cs','LauncherShell.cs',
        'UpgradeSupport.cs','UpgradeTests.cs','MixedSourceRecovery.cs','InstallationRemoval.cs',
        'LauncherLayoutTests.cs','RuntimeSupport.cs','LauncherUpdate.cs',
        'Tests\LauncherRedesignHarness.cs') | ForEach-Object { Join-Path $root $_ }
    $refs = @('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll',
        '/r:System.Core.dll','/r:System.Management.dll','/r:System.IO.Compression.dll',
        '/r:System.IO.Compression.FileSystem.dll')
    $resources = @(
        ('/resource:'+(Join-Path $root 'NativeReferenceFiles.json')+',Celesphonia.NativeReferenceFiles.json'),
        ('/resource:'+(Join-Path $root 'NativeReferenceFiles-ch-current.json')+',Celesphonia.NativeReferenceFiles.ch-current.json'),
        ('/resource:'+(Join-Path $root 'NativeReferenceFiles-maple-1-7-2.json')+',Celesphonia.NativeReferenceFiles.maple-1-7-2.json'),
        ('/resource:'+(Join-Path $root 'NativeReferenceFiles-three-in-one-3-3-4.json')+',Celesphonia.NativeReferenceFiles.three-in-one-3-3-4.json'),
        ('/resource:'+(Join-Path $root 'Vendor\SevenZip\7z.exe')+',Celesphonia.Rar7z.exe'),
        ('/resource:'+(Join-Path $root 'Vendor\SevenZip\7z.dll')+',Celesphonia.Rar7z.dll')
    )
    $english = Join-Path $root 'Tests\embedded-english-release.zip'
    $resources += @(Get-ChildItem -LiteralPath $root -Filter 'NativeReferenceFiles-three-in-one-3-3-5-*.json' -File | ForEach-Object { '/resource:' + $_.FullName + ',Celesphonia.NativeReferenceFiles.' + $_.BaseName.Substring('NativeReferenceFiles-'.Length) + '.json' })
    if(Test-Path -LiteralPath $english){
        $resources += '/resource:'+$english+',Celesphonia.EnglishRelease.zip'
    }
    $exe = Join-Path $temporary 'LauncherRedesignHarness.exe'
    & $compiler /nologo /target:exe /main:LauncherRedesignHarness ('/out:'+$exe) $refs $resources $source
    if($LASTEXITCODE -ne 0){throw 'Native QA compilation failed.'}
    $cases = @(
        @{name='legacy-real';fixture='real-fixture.json';extra=@('legacy');output='legacy-real-report.json'},
        @{name='legacy-empty';fixture='empty-fixture.json';extra=@('legacy');output='legacy-empty-report.json'}
    )
    if(!$OnlyLegacy) {
        $cases = @(@{name='redesign';fixture='real-fixture.json';extra=@();output='redesign-native-report.json'})+$cases
    }
    $runs = @()
    foreach($case in $cases){
        $report = Join-Path $Results $case.output
        try {
            & $exe $report (Join-Path $temporary $case.fixture) @($case.extra)
            $code=$LASTEXITCODE
        } catch {
            $code=-1
            Write-Warning ('UI test '+$case.name+' raised '+$_.Exception.Message)
        }
        $runs += [pscustomobject]@{test=$case.name;exitCode=$code;report=$report;exists=(Test-Path $report)}
        Write-Host ($case.name+' exit='+$code+' report='+$report)
    }
    $runs | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $Results 'native-qa-test-runs.json') -Encoding UTF8
    if(@($runs | Where-Object {$_.exitCode -ne 0}).Count -gt 0){exit 1}
} finally {
    if((Split-Path -Parent $temporary) -eq $env:TEMP -and
       (Split-Path -Leaf $temporary) -match '^Cph-native-layout-[0-9a-f]{32}$' -and
       (Test-Path -LiteralPath $temporary)){
        Remove-Item -LiteralPath $temporary -Recurse -Force
    }
}
