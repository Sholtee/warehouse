#
# Deploy-App.ps1
#
# Usage: Deploy-App.ps1 -action [create|update] -prefix prefix -region region-name -profile profile-name [-skipImageUpdate]
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

  [switch]$skipImageUpdate = $false
)

$ErrorActionPreference = "Stop"

$stackName = "${prefix}-warehouse-app"

if (!$skipImageUpdate) {
  $PATH = [System.IO.Path]

  $ecrHost = "$(aws sts get-caller-identity --profile ${profile} --region ${region} --query Account --output text).dkr.ecr.${region}.amazonaws.com"

  function Build-Image([string]$context, [string]$image, [string]$dockerfile) {
    $context = $PATH::Combine($PSScriptRoot, '..', 'SRC', $context) | Resolve-Path

    $global:image = "${ecrHost}/${prefix}-warehouse-ecr-repository:${image}-$((New-Guid).ToString('N'))"

    docker build `
      --file $($PATH::Combine($context, $dockerfile) | Resolve-Path) `
      --build-arg CONFIG=Release `
      --platform linux/amd64 `
      --force-rm `
      --tag $image `
      $context
  }

  aws ecr get-login-password --profile ${profile} --region ${region} | docker login --username AWS --password-stdin ${ecrHost}
  try {
    Build-Image -context App -image $stackName -dockerfile dockerfile
    docker push $image

    Build-Image -context Tools -image ${prefix}-warehouse-db-migratorp -dockerfile dockerfile-dbmigrator
    docker push $image
  } finally {
    docker logout ${ecrHost}
  }

  $imageParam = "ParameterValue=${image}"
} else {
  $imageParam = "UsePreviousValue=true"
}

#aws cloudformation ${action}-stack `
#  --profile $profile `
#  --stack-name $stackName `
#  --region $region `
#  --template-body file://./app.yml `
#  --parameters "ParameterKey=prefix,ParameterValue=${prefix}" "ParameterKey=image,${imageParam}" `
#  --capabilities CAPABILITY_NAMED_IAM