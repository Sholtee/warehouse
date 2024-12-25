#
# Create-Certs.ps1
#
# Author: Denes Solti
# Project: Warehouse API (boilerplate)
# License: MIT
#

function Combine-Path([Parameter(Position = 0)][string[]]$path) {
  return [System.IO.Path]::Combine($path)
}

function OpenSSL([Parameter(Position=0, Mandatory=$true)][string]$args) {
  Start-Process `
    -FilePath (Combine-Path $Env:ProgramFiles, 'Git', 'usr', 'bin', 'openssl.exe') `
    -ArgumentList ${args} `
    -NoNewWindow `
    -Wait
}

 OpenSSL "genrsa -out root-ca.key 2048"
 OpenSSL "req -x509 -new -key root-ca.key -days 3650 -out root-ca.crt -subj '/C=HU/ST=BA/L=Pecs/O=Denes Solti CA/CN=Denes Solti CA/emailAddress=ca@denes.solti.com'"
 OpenSSL "genrsa -out client.key 2048"
 OpenSSL "req -new -key client.key -out client.csr -subj '/C=HU/ST=BA/L=Pecs/O=Warehouse/CN=Warehouse/emailAddress=warehouse@warehouse.com'"
 OpenSSL "x509 -req -days 3650 -CA root-ca.crt -CAkey root-ca.key -CAcreateserial -CAserial serial -in client.csr -out client.crt"