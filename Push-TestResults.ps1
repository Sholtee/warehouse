#
# Push-TestResults.ps1
#
# Author: Denes Solti
# Project: Warehouse API (boilerplate)
# License: MIT
#

$ErrorActionPreference = "Stop"

$PATH=[System.IO.Path]

$artifacts=($PATH::Combine("$(Get-Location)", 'Artifacts') | Resolve-Path)
$client=New-Object System.Net.WebClient

Get-ChildItem -Path $PATH::Combine($artifacts, "*.Tests.Results.xml") | foreach {
  Write-Host "Uploading test result: $($_.Name)"
  $client.UploadFile("https://ci.appveyor.com/api/testresults/nunit/$Env:APPVEYOR_JOB_ID", $_.FullName)
}

dotnet tool install --global coveralls.net --version 4.0.1

csmacnz.Coveralls `
  --dynamiccodecoverage -i `"$($PATH::Combine($artifacts, "Coverage.xml"))`" `
  --repoToken $Env:COVERALLS_REPO_TOKEN  `
  --commitId $Env:APPVEYOR_REPO_COMMIT `
  --commitBranch $Env:APPVEYOR_REPO_BRANCH `
  --commitAuthor `"$Env:APPVEYOR_REPO_COMMIT_AUTHOR`" `
  --commitEmail $Env:APPVEYOR_REPO_COMMIT_AUTHOR_EMAIL `
  --commitMessage `"$Env:APPVEYOR_REPO_COMMIT_MESSAGE`" `
  --serviceName appveyor `
  --serviceNumber $Env:APPVEYOR_BUILD_NUMBER `
  --useRelativePaths