#
# Run-Tests.ps1
#
# Author: Denes Solti
# Project: Warehouse API (boilerplate)
# License: MIT
#

$ErrorActionPreference = "Stop"

dotnet tool install --global dotnet-coverage --version 17.13.1

$PATH = [System.IO.Path]
$artifacts = $PATH::Combine("$(Get-Location)", 'Artifacts')

dotnet-coverage collect `
  --settings $PATH::Combine("$(Get-Location)", 'CoverageSettings.xml') `
  --output $PATH::Combine($artifacts, 'Coverage.xml') `
  "dotnet test --test-adapter-path:. --logger:nunit;LogFilePath=$($PATH::Combine($artifacts, '{assembly}.Results.xml'))"

if (!$?) {
  throw "Test session failed"
}

dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.4.1

reportgenerator `
  -reports:$PATH::Combine($artifacts, 'Coverage.xml') `
  -targetdir:$PATH::Combine($artifacts, 'CoverageReport') `
  -reporttypes:Html_Dark