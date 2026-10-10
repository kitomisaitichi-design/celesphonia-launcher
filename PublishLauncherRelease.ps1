param(
    [Parameter(Mandatory=$true)][string]$Repository,
    [string]$Version='1.0.5',
    [switch]$Publish
)
# This script only publishes launcher packages. Game/mod RARs, private
# profiles, saves, and local F8 sessions must never enter GitHub releases.
$ErrorActionPreference='Stop'
$project=$PSScriptRoot
$release=Join-Path (Split-Path $project -Parent) 'Celesphonia-Delta-Launcher-CH1.zip'
$checksum=$release+'.sha256.txt'
if($Repository -notmatch '^[A-Za-z0-9][A-Za-z0-9-]{0,38}/[A-Za-z0-9_.-]+$'){
    throw 'Provide the existing GitHub repository as owner/repository.'
}
if($Version -notmatch '^\d+\.\d+\.\d+$'){throw 'Launcher releases must use semantic version numbers.'}
$source=Get-Content -LiteralPath (Join-Path $project 'LauncherUpdate.cs') -Raw
if(-not $source.Contains('CurrentVersion="'+$Version+'"')){throw 'Requested release version does not match the compiled launcher source.'}
if(-not(Test-Path -LiteralPath $release)){throw 'Build and verify the canonical portable ZIP first.'}
$py=Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
if(-not(Test-Path -LiteralPath $py)){throw 'Python packaging verifier is missing.'}
$check=@'
import hashlib,sys,zipfile
f=sys.argv[1]
with zipfile.ZipFile(f) as z:
    assert z.testzip() is None
    names=set(z.namelist())
    assert 'Celesphonia Portable Switcher.exe' in names
    assert 'mod-recipes.json' in names
    assert 'profiles.json' not in names
    assert not any(n.startswith(('Save Backups/','Game Profiles/','Control Sessions/','Staging/')) for n in names)
print(hashlib.sha256(open(f,'rb').read()).hexdigest())
'@
$digest=$check|& $py - $release
if($LASTEXITCODE -ne 0 -or $digest -notmatch '^[a-f0-9]{64}$'){throw 'Distribution checksum or privacy validation failed.'}
Set-Content -LiteralPath $checksum -Value ($digest+'  Celesphonia-Delta-Launcher-CH1.zip') -Encoding Ascii
Write-Output ('Verified launcher ZIP: '+$release)
Write-Output ('SHA-256 '+$digest)
Write-Output ('Checksum asset: '+$checksum)
if(-not $Publish){
    Write-Output ('Prepared for '+$Repository+'. To publish to GitHub Releases, rerun with -Publish.')
    exit 0
}
$gh=Get-Command gh -ErrorAction SilentlyContinue
if(-not $gh){throw 'GitHub CLI (gh) is not installed. Authenticate with gh auth login before publishing.'}
& $gh.Source auth status
if($LASTEXITCODE -ne 0){throw 'GitHub CLI is not authenticated.'}
& $gh.Source repo view $Repository --json nameWithOwner
if($LASTEXITCODE -ne 0){throw 'GitHub repository does not exist or is inaccessible. Nothing was published.'}
$notes=Join-Path $project 'LAUNCHER AUTO-UPDATE.md'
& $gh.Source release create ('launcher-v'+$Version) $release $checksum --repo $Repository --title ('Celesphonia Launcher v'+$Version) --notes-file $notes
if($LASTEXITCODE -ne 0){throw 'GitHub release creation failed; inspect the existing release before retrying.'}
Write-Output 'Release published. In the launcher, set Help → GitHub source to this exact owner/repository.'
