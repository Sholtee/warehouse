#
# Deploy-Foundation.ps1
#
# Usage: Deploy-Foundation.ps1 -action [create|update] -prefix prefix -region region-name -profile profile-name -certificate cert.crt -privateKey private.key
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

  [Parameter(Position=2, Mandatory=$true)]
  [string]$region,

  [Parameter(Position=3, Mandatory=$true)]
  [string]$profile,

  [Parameter(Position=4, Mandatory=$true)]
  [ValidatePattern("^.+\.crt$")]
  [string]$certificate,

  [Parameter(Position=5, Mandatory=$true)]
  [ValidatePattern("^.+\.key$")]
  [string]$privateKey
)

$ErrorActionPreference = 'Stop'

$stackName = "${prefix}-warehouse-foundation"

aws cloudformation ${action}-stack `
  --profile ${profile} `
  --region ${region} `
  --stack-name ${stackName} `
  --template-body file://./foundation.yml `
  --parameters (./Read-Config.ps1 ./foundation.${prefix}.json) `
  --capabilities CAPABILITY_NAMED_IAM CAPABILITY_AUTO_EXPAND

aws cloudformation wait stack-update-complete `
  --profile $profile `
  --region $region `
  --stack-name $stackName

# We don't want the certificate to be stored in CloudFormation parameter list so copy it directly
aws secretsmanager put-secret-value `
  --profile $profile `
  --region $region `
  --secret-id ${prefix}-warehouse-app-cert `
  --secret-string "{`"privateKey`": `"$(Get-Content -Path $certificate)`", `"certificate`": `"$(Get-Content -Path $privateKey)`"}"