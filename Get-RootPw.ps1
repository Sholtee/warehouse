#
# Get-RootPw.ps1
#
# Author: Denes Solti
# Project: Warehouse API (boilerplate)
# License: MIT
#

Write-Host (aws secretsmanager get-secret-value --secret-id local-root-user-creds --endpoint-url http://localhost:4566 --region local | ConvertFrom-Json).SecretString