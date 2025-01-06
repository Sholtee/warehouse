#
# Deploy-Foundation.ps1
#
# Usage: Deploy-Foundation.ps1 -action [create|update] -prefix prefix -region region-name -profile profile-name -certificate cert.crt -privateKey private.key [-deploymentId ...]
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
  [string]$privateKey,

  [Parameter(Position=5)]
  [Guid]$deploymentId = (New-Guid)
)

$ErrorActionPreference = 'Stop'

$stackName = "${prefix}-warehouse-foundation"

aws cloudformation ${action}-stack `
  --profile ${profile} `
  --region ${region} `
  --stack-name ${stackName} `
  --template-body file://./foundation.yml `
  --parameters (./Read-Config.ps1 ./foundation.${prefix}.json -extra @{deploymentId=$deploymentId}) `
  --capabilities CAPABILITY_NAMED_IAM CAPABILITY_AUTO_EXPAND

aws cloudformation wait stack-${action}-complete `
  --profile $profile `
  --region $region `
  --stack-name $stackName

# We don't want the certificate to be stored in CloudFormation parameter list so copy it directly
aws secretsmanager put-secret-value `
  --profile $profile `
  --region $region `
  --secret-id ${prefix}-warehouse-app-cert `
  --secret-string $(@{certificate=(Get-Content -Path $certificate -Raw); privateKey=(Get-Content -Path $privateKey -Raw)} | ConvertTo-Json)