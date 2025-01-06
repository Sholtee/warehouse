#
# Read-Config.ps1
#
# Usage: Read-Config.ps1 -path path/to/config.json [-extra @{key1=value1; key2="value2"}]
#
# Author: Denes Solti
# Project: Warehouse API (boilerplate)
# License: MIT
#

param(
  [Parameter(Position=0, Mandatory=$true)]
  [string]$path,

  [Parameter(Position=1)]
  [HashTable]$extra = @{}
)

$ErrorActionPreference = "Stop"

$config = (Get-Content $path -Raw | ConvertFrom-Json -AsHashTable) + $extra

$config.GetEnumerator() | ForEach-Object `
  -Begin {$params=""} `
  -Process {
      $value = $_.Value
      if ($value -Is [HashTable]) {
          $value = (Get-Content $value.read -Raw)
      }

      $params += "`"ParameterKey=$($_.Key),ParameterValue=$value`" "
  }

return $params