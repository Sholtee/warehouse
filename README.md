# Warehouse API (boilerplate) [![Build status](https://ci.appveyor.com/api/projects/status/na8ucgrrf5g34202/branch/master?svg=true)](https://ci.appveyor.com/project/Sholtee/warehouse/branch/master) ![AppVeyor tests](https://img.shields.io/appveyor/tests/sholtee/warehouse/master) [![Coverage Status](https://coveralls.io/repos/github/Sholtee/warehouse/badge.svg?branch=master)](https://coveralls.io/github/Sholtee/warehouse?branch=master)

> REST API over ASP.NET Core running on AWS infra 

## Architecture
![Architecture](Assets/Architecture/architecture.png)

## Stack
- FW: ASP.NET Core, .NET 9
- DB: MySQL
- ORM: [ServiceStack ORMLite](https://docs.servicestack.net/ormlite/)
- Migration [DBUp](https://dbup.github.io/)
- Logging: [Serilog](https://serilog.net/)
- Mapping: [AutoMapper](https://automapper.org/)
- API explorer: [Swashbuckle/Swagger](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/)
- Infra: [AWS](https://aws.amazon.com/), [LocalStack](https://www.localstack.cloud/)
- Test FW: [NUnit](https://nunit.org/)

## Authentication
The API uses stateless authentication (bearer token in session cookie)

![Auth flow](Assets/Auth/auth.png)

## Using the local environment
Requirements
- [PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows?view=powershell-7.4)
- [Docker](https://docs.docker.com/desktop/setup/install/windows-install/)
- [Git](https://git-scm.com/downloads/win) (OpenSSL binaries provided by Git are used during the setup process, for more details see `.\SRC\Tools\LocalStackSetup\Cert\Create-Certs.ps1`)

Launching the app
- (Optional) Set up the `root` password by changing the value of `services.localstack-setup.environment.ROOT_PASSWORD` in `docker-compose.yml`
- Run `.\Run-Local.ps1`

To access the app via API explorer go to [https://localhost:1986/](https://localhost:1986/)

To query items using cURL:
- `curl --location 'https://localhost:1986/api/v1/login' --header 'Authorization: Basic cm9vdDptZWR2ZWRpc3pub2VtYmVy'`
- Grab the session token from the `Set-Cookie` header 
- List the 3rd page of items satisfying the following filter: `(Brand == "Samsung" && "Price" < 1000) || (Brand == "Sony" && "Price" < 1500)`
  ```
  curl --location 'https://localhost:1986/api/v1/products' \
    --header 'Content-Type: application/json' \
    --header 'Cookie: warehouse-session=eyJhbGc...' \
    --data '{
      "filter": {
	    "block": {
	      "string": {
	  	    "property": "Name",
		    "comparison": "equals",
		    "value": "Samsung"
	      },
	      "and": {
		    "decimal": {
		      "property": "Price",
		      "value": 1000,
		      "comparison": "lessThan"
		    }
	      }
	    },
	    "or": {
	      "block": {
		    "string": {
		      "property": "Name",
		      "comparison": "equals",
		      "value": "Sony"
		    },
		    "and": {
		      "decimal": {
			    "property": "Price",
			    "value": 1500,
			    "comparison": "lessThan"
		      }
		    }
	      }
	    }
      },
      "sortBy": {
	    "properties": [
	      {
		    "property": "Name",
		    "kind": "ascending"
	      },
	      {
		    "property": "Price",
		    "kind": "descending"
	      }
	    ]
      },
      "page": {
	    "skip": 3,
	    "size": 5
      }
    }'
  ```
  
## Deploying the app to AWS
- `.\CloudFormation\Deploy-Foundation.ps1 -action [create|update] -prefix [dev|test|prod] -region region-name -profile profile-name -certificate cert.crt -privateKey private.key`
- `.\CloudFormation\Deploy-Migrator.ps1 -action [create|update] -prefix [dev|test|prod] -region region-name -profile profile-name [-runMigrations]`
- `.\CloudFormation\Deploy-App.ps1 -action [create|update] -prefix [dev|test|prod] -region region-name -profile profile-name [-skipImageUpdate]`