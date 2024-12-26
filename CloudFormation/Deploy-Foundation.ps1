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
  --stack-name ${stackName} `
  --region ${region} `
  --template-body file://./foundation.yml `
  --parameters "ParameterKey=prefix,ParameterValue=${prefix}" "ParameterKey=certificate,ParameterValue=$(Get-Content -Path $certificate)" "ParameterKey=privateKey,ParameterValue=$(Get-Content -Path $privateKey)"`
  --capabilities CAPABILITY_NAMED_IAM CAPABILITY_AUTO_EXPAND

aws cloudformation wait stack-${action}-complete --profile ${profile} --region ${region} --stack-name ${stackName}