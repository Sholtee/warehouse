#
# Run-Tests.ps1
#
# Author: Denes Solti
# Project: Warehouse API (boilerplate)
# License: MIT
#

$ErrorActionPreference = "Stop"

dotnet tool install --global dotnet-coverage --version 17.13.1

$Path=[System.IO.Path]
$artifacts=$Path::Combine("$(Get-Location)", 'Artifacts')

dotnet-coverage collect `
  --settings $Path::Combine("$(Get-Location)", 'CoverageSettings.xml') `
  --output $Path::Combine($artifacts, 'Coverage.xml') `
  "dotnet test --test-adapter-path:. --logger:nunit;LogFilePath=$($Path::Combine($artifacts, '{assembly}.Results.xml'))"

dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.4.1

reportgenerator `
  -reports:$Path::Combine($artifacts, 'Coverage.xml') `
  -targetdir:$Path::Combine($artifacts, 'CoverageReport') `
  -reporttypes:Html_Dark