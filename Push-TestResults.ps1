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
  $client.UploadFile("https://ci.appveyor.com/api/testresults/$type/$Env:APPVEYOR_JOB_ID", $_.FullName)
}