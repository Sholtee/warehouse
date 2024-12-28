#
# Run-Local.ps1
#
# Author: Denes Solti
# Project: Warehouse API (boilerplate)
# License: MIT
#

$ErrorActionPreference = "Stop"

./SRC/Tools/LocalStackSetup/Cert/Create-Certs.ps1

./CloudFormation/Package-Lambda.ps1 ./SRC/Tools/DbMigrator/DbMigrator.csproj

docker compose up