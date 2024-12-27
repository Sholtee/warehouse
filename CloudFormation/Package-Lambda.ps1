#
# Package-Lambda.ps1
#
# Usage: Package-Lambda.ps1 -csproj path/to.csproj
#
# Author: Denes Solti
# Project: Warehouse API (boilerplate)
# License: MIT
#

param(
  [Parameter(Position=0, Mandatory=$true)]
  [string]$csproj
)

$ErrorActionPreference = "Stop"

$PATH = [System.IO.Path]

$tmpDir = $PATH::Combine($PSScriptRoot, ".tmp")
$binDir = $PATH::Combine($tmpDir, "bin")

$csproj = $csproj | Resolve-Path

dotnet publish $csproj -c Release -o $binDir -r "linux-x64" -p:PublishReadyToRun=true
try {
  Compress-Archive -Path (Join-Path $binDir '*') -DestinationPath $PATH::Combine($tmpDir, "$($PATH::GetFileNameWithoutExtension($csproj)).zip")
} finally {
  Remove-Item $binDir -Recurse -Force
}