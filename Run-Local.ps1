#
# Run-Local.ps1
#
# Author: Denes Solti
# Project: Warehouse API (boilerplate)
# License: MIT
#

$ErrorActionPreference = "Stop"

./CloudFormation/Package-Lambda.ps1 ./SRC/Tools/DbMigrator/DbMigrator.csproj

docker compose up