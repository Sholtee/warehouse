#
# Deploy-DbMigrator.ps1
#
# Usage: Deploy-Migrator.ps1 -action [create|update] -prefix prefix -region region-name -profile profile-name
#
# Author: Denes Solti
# Project: Warehouse API (boilerplate)
# License: MIT
#

param(
  [Parameter(Position=0, Mandatory=$true)]
  [string]$action,

  [Parameter(Position=1, Mandatory=$true)]
  [string]$prefix,

  [Parameter(Position=3, Mandatory=$true)]
  [string]$profile,

  [Parameter(Position=4, Mandatory=$true)]
  [string]$region
)

$ErrorActionPreference = "Stop"

$PATH = [System.IO.Path]

./Package-Lambda.ps1 ($PATH::Combine('..', 'SRC', 'Tools', 'DbMigrator', 'DbMigrator.csproj') | Resolve-Path)

$version = "$((New-Guid).ToString('N'))"

aws s3 cp `
  --profile $profile `
  --region $region `
  ($PATH::Combine('.', '.tmp', 'DbMigrator.zip') | Resolve-Path) s3://${prefix}-warehouse-lambda-binaries/${prefix}-warehouse-db-migrator-${version}.zip

aws cloudformation ${action}-stack `
  --profile $profile `
  --stack-name "${prefix}-warehouse-db-migrator" `
  --region $region `
  --template-body file://./db-migrator.yml `
  --parameters "ParameterKey=prefix,ParameterValue=${prefix}" "ParameterKey=lambdaVersion,ParameterValue=${version}" `
  --capabilities CAPABILITY_NAMED_IAM