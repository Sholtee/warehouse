#
# Deploy-App.ps1
#
# Usage: Deploy-App.ps1 -action [create|update] -prefix prefix -region region-name -profile profile-name [-deploymentId ...]
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

  [Parameter(Position=5)]
  [Guid]$deploymentId = (New-Guid)
)

$ErrorActionPreference = "Stop"

$PATH = [System.IO.Path]

$stackName = "${prefix}-warehouse-app"
$ecrHost = "$(aws sts get-caller-identity --profile ${profile} --region ${region} --query Account --output text).dkr.ecr.${region}.amazonaws.com"

aws ecr get-login-password --profile $profile --region $region | docker login --username AWS --password-stdin $ecrHost
try {
  $context = $PATH::Combine($PSScriptRoot, '..', 'SRC', 'App') | Resolve-Path
  $image = "${ecrHost}/${prefix}-warehouse-ecr-repository:${stackName}-${deploymentId}"

  docker build `
    --file $($PATH::Combine($context, 'dockerfile') | Resolve-Path) `
    --build-arg CONFIG=Release `
    --platform linux/amd64 `
    --force-rm `
    --tag $image `
    $context

  docker push $image
  docker rmi $image --force
} finally {
  docker logout ${ecrHost}
}

aws cloudformation ${action}-stack `
  --profile $profile `
  --stack-name $stackName `
  --region $region `
  --template-body file://./app.yml `
  --parameters (./Read-Config.ps1 ./app.${prefix}.json -extra @{deploymentId=$deploymentId}) `
  --capabilities CAPABILITY_NAMED_IAM