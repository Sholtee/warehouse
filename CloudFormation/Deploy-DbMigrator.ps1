#
# Deploy-DbMigrator.ps1
#
# Usage: Deploy-Migrator.ps1 -action [create|update] -prefix prefix -region region-name -profile profile-name [-runMigrations]
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
  [string]$region,

  [switch]$runMigrations = $false
)

$ErrorActionPreference = "Stop"

$PATH = [System.IO.Path]

./Package-Lambda.ps1 ($PATH::Combine('..', 'SRC', 'Tools', 'DbMigrator', 'DbMigrator.csproj') | Resolve-Path)

$version = "$((New-Guid).ToString('N'))"

aws s3 cp `
  --profile $profile `
  --region $region `
  ($PATH::Combine('.', '.tmp', 'DbMigrator.zip') | Resolve-Path) s3://${prefix}-warehouse-lambda-binaries/${prefix}-warehouse-db-migrator-${version}.zip

$stackName = "${prefix}-warehouse-db-migrator"

aws cloudformation ${action}-stack `
  --profile $profile `
  --stack-name  $stackName`
  --region $region `
  --template-body file://./db-migrator.yml `
  --parameters "ParameterKey=prefix,ParameterValue=${prefix}" "ParameterKey=lambdaVersion,ParameterValue=${version}" `
  --capabilities CAPABILITY_NAMED_IAM

if ($runMigrations) {
  aws cloudformation wait stack-${action}-complete `
    --profile $profile `
    --region $region `
    --stack-name $stackName

  aws lambda invoke `
    --function-name "${prefix}-warehouse-db-migrator-lambda" `
    --profile $profile `
    --region $region `
    ($PATH::Combine('.', '.tmp', "db-migrator-invocation-$((New-Guid).ToString('N')).log"))
}