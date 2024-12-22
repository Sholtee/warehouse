#
# RunTests.ps1
#
# Author: Denes Solti
# Project: Warehouse API (boilerplate)
# License: MIT
#

dotnet tool install --global dotnet-coverage --version 17.13.1

$Path=[System.IO.Path]
$artifacts=$Path::Join("$(Get-Location)", 'Artifacts')

dotnet-coverage collect `
	--settings "$($Path::Join("$(Get-Location)", 'CoverageSettings.xml'))" `
	--output "$($Path::Join($artifacts, 'Coverage.xml'))" `
	"dotnet test --test-adapter-path:. --logger:nunit;LogFilePath=$($Path::Join($artifacts, '{assembly}.Results.xml'))"

dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.4.1

reportgenerator "-reports:$($Path::Join($artifacts, 'Coverage.xml'))" "-targetdir:$($Path::Join($artifacts, 'CoverageReport'))" -reporttypes:Html_Dark