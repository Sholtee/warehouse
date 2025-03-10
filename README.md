# Warehouse API (boilerplate) [![Build status](https://ci.appveyor.com/api/projects/status/na8ucgrrf5g34202/branch/master?svg=true)](https://ci.appveyor.com/project/Sholtee/warehouse/branch/master) ![AppVeyor tests](https://img.shields.io/appveyor/tests/sholtee/warehouse/master) [![Coverage Status](https://coveralls.io/repos/github/Sholtee/warehouse/badge.svg?branch=master)](https://coveralls.io/github/Sholtee/warehouse?branch=master)

> REST API over ASP.NET Core running on AWS infra 

## Architecture
![Architecture](Assets/Architecture/architecture.png)

## Stack
- FW: ASP.NET Core, .NET 9
- DB: MySQL + [MySqlConnector](https://mysqlconnector.net/)
- ORM: [ServiceStack ORMLite](https://docs.servicestack.net/ormlite/)
- Cache: Redis + [StackExchange Redis](https://stackexchange.github.io/StackExchange.Redis/)
- Migration [DBUp](https://dbup.github.io/)
- Logging: [Serilog](https://serilog.net/)
- Mapping: [AutoMapper](https://automapper.org/)
- Profiling: [MiniProfiler](https://miniprofiler.com/dotnet/)
- Throttling: [RedisRateLimiting](https://www.nuget.org/packages/RedisRateLimiting)
- API explorer: [Swashbuckle/Swagger](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/)
- Infra: [AWS](https://aws.amazon.com/), [Docker](https://www.docker.com/products/docker-desktop/), [LocalStack](https://www.localstack.cloud/)
- Test FW: [NUnit](https://nunit.org/)

## Folder structure
```
root
│
└───Artifacts [test ouputs]
│
└───Assets [documentation assets: diagrams, images, etc]
│
└───CloudFormation [CloudFormation templates & deployment scripts]
│
└───BIN [application binaries]
│   │
│   └───Debug|Release
│       │
│       └───net8.0 [DB Migrator lambda binaries - net9.0 is not supported in AWS Lambda]
│       │
│       └───net9.0 [application & test binaries]
│
└───SRC [sources root]
│   │
│   └───App [application sources]
│   │   │
│   │   └───Warehouse.API [business logic]
│   │   │   │
│   │   │   └───Controllers [controllers home]
│   │   │       │
│   │   │       └───[controllers name]
│   │   │           │
│   │   │           └───Dtos [data-transfer-objects]
│   │   │           │
│   │   │           └───Examples [Swagger example descriptors]
│   │   │           │
│   │   │           └───Profiles [AutoMapper profiles, for instance API DTO -> DAL DTO]
│   │   │
│   │   └───Warehouse.Core [common resoures]
│   │   │   │
│   │   │   └───Abstractions [interfaces, abstract classes]
│   │   │   │
│   │   │   └───Attributes [attributes]
│   │   │   │
│   │   │   └───Auth [authentication related stuffs]
│   │   │   │
│   │   │   └───Exceptions [exceptions]
│   │   │   │
│   │   │   └───Extensions [extension methods]
│   │   │
│   │   └───Warehouse.DAL [data access layer]
│   │   │   │
│   │   │   └───Extensions [private extension methods]
│   │   │   │
│   │   │   └───Repositories [repositories home]
│   │   │       │
│   │   │       └───[repository name]
│   │   │           │
│   │   │           └───Dtos [data-transfer-objects]
│   │   │           │
│   │   │           └───Entities [ORM database entities]
│   │   │           │
│   │   │           └───Views [ORM database views]
│   │   │
│   │   └───Warehouse.Host [application host]
│   │       │
│   │       └───Dtos [data-transfer-objects related to the host, such as HealthCheckResult or ErrorDetails]
│   │       │
│   │       └───Infrastructure [host infrastructure]
│   │       │   │
│   │       │   └───Auth [authentication handler related stuffs]
│   │       │   │
│   │       │   └───Config [configuration helpers]
│   │       │   │
│   │       │   └───Filters [ASP.NET & Swashbuckle related filters]
│   │       │   │
│   │       │   └───Middlewares [ASP.NET middlewares]
│   │       │   │
│   │       │   └───Profiling [profiling related helper classes]
│   │       │   │
│   │       │   └───Registrations [service registrations]
│   │       │
│   │       └───Services [internal services]
│   │           │
│   │           └───Auth [authentication related services]
│   │           │
│   │           └───HealthCheck [healthcheck related services]
│   │
│   └───Tools [tooling]
│       │
│       └───LocalStackSetup [initializes the infra on the dev machine, used only locally]
│       │
│       └───DbMigrator [AWS Lambda, that runs the database migration scripts]
│
└───TESTS [test sources]
```

## Authentication
The API can use [stateful](https://github.com/Sholtee/warehouse/blob/115339c9ff4344ad47e279c92b50795b51dfef12/SRC/App/Warehouse.Host/Infrastructure/Registrations/Registrations.Authentication.cs#L38) (session id in session cookie) or [stateless](https://github.com/Sholtee/warehouse/blob/115339c9ff4344ad47e279c92b50795b51dfef12/SRC/App/Warehouse.Host/Infrastructure/Registrations/Registrations.Authentication.cs#L51) (bearer token in session cookie) authentication.
Sliding expiration also supported but using it together with stateless authentication is strongly not advisable as it may let the client to create an "infinite" token

![Auth flow](Assets/Auth/auth.png)

Session management can be fine-tuned via [config](https://github.com/Sholtee/warehouse/blob/115339c9ff4344ad47e279c92b50795b51dfef12/SRC/App/Warehouse.Host/appsettings.json#L18)

## Using the local environment
Requirements
- [PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows?view=powershell-7.4)
- [Docker](https://docs.docker.com/desktop/setup/install/windows-install/)
- [Git](https://git-scm.com/downloads/win) (OpenSSL binaries provided by Git are used during the setup process, for more details see `.\SRC\Tools\LocalStackSetup\Cert\Create-Certs.ps1`)

Launching the app
- (Optional) Set up the `root` password by changing the value of `services.localstack-setup.environment.ROOT_PASSWORD` in `docker-compose.yml`
- Run `.\Run-Local.ps1`

To query items using cURL:
- `curl --location 'https://localhost:1986/api/v1/login' --header 'Authorization: Basic cm9vdDptZWR2ZWRpc3pub2VtYmVy'`
- Grab the session token from the `Set-Cookie` header 
- List the 1st page of items satisfying the following filter: `(Brand == "Samsung" && "Price" < 1000) || (Brand == "Sony" && "Price" < 1500)`
  ```
  curl --location 'https://localhost:1986/api/v1/products' \
    --header 'Content-Type: application/json' \
    --header 'Cookie: warehouse-session=eyJhbGc...' \
    --data '{
      "filter": {
        "block": {
          "string": {
            "property": "Brand",
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
              "property": "Brand",
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
        "skip": 0,
        "size": 5
      }
    }'
  ```

## API explorer
- Available at `<base_url>/` (defaults to [https://localhost:1986/](https://localhost:1986/))
- Can be disabled from configuration by removing the [Swagger section](https://github.com/Sholtee/warehouse/blob/4570720e4c2decb051d1155c16ff1fa253da7446/SRC/App/Warehouse.Host/appsettings.local.json#L7)

## Throttling 
- Login endpoints are protected by [fixed time window rate limiter](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-9.0#fixed-window-limiter ), set to allow [100 requests / minute](https://github.com/Sholtee/warehouse/blob/61feabed42df1d2f99d96574e89b575950d56f7a/SRC/App/Warehouse.Host/appsettings.json#L33)
- Business logic endpoints are protected by a modified [token bucket limiter](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-9.0#token-bucket-limiter) where each user has its [own bucket](https://github.com/Sholtee/warehouse/blob/b2bdd7664f0ff206b303f265e0075a8ac8fe8562/SRC/App/Warehouse.Host/Infrastructure/Registrations/Registrations.RateLimiting.cs#L50). By default this limiter is set to allow [10 requests / minute / user](https://github.com/Sholtee/warehouse/blob/61feabed42df1d2f99d96574e89b575950d56f7a/SRC/App/Warehouse.Host/appsettings.json#L38 )
- Note that the rate-limiting is Redis based so the number of active API nodes won't mess up the throttling
 
## Health checks
- Available at `<base_url>/healthcheck` (defaults to [https://localhost:1986/healthcheck](https://localhost:1986/))
- It executes [database connection](https://github.com/Sholtee/warehouse/blob/6aefec85f1fe7aa28c5f7d3097f16d40b2742b7d/SRC/App/Warehouse.Host/Services/HealthCheck/DbConnectionHealthCheck.cs#L18), [Redis](https://github.com/Sholtee/warehouse/blob/6aefec85f1fe7aa28c5f7d3097f16d40b2742b7d/SRC/App/Warehouse.Host/Services/HealthCheck/RedisHealthCheck.cs#L17) and [aws client](https://github.com/Sholtee/warehouse/blob/6aefec85f1fe7aa28c5f7d3097f16d40b2742b7d/SRC/App/Warehouse.Host/Services/HealthCheck/AwsHealthCheck.cs#L18) checks
- The endpoint is invoked during [container](https://github.com/Sholtee/warehouse/blob/0ebba5ee75d9338dfa0810ccadf94242437d424c/SRC/App/dockerfile#L28) and [service](https://github.com/Sholtee/warehouse/blob/0ebba5ee75d9338dfa0810ccadf94242437d424c/CloudFormation/app.yml#L90) health checks
- The endpoint is NOT authenticated therefore should not be exposed to clients

## Profiling
- Available at `<base_url>/profiler/results-index` (defaults to [https://localhost:1986/profiler/results-index](https://localhost:1986/profiler/results-index))
- Can be disabled from [configuration](https://github.com/Sholtee/warehouse/blob/730f3003f113bce393b899520cf610ed6c290845/SRC/App/Warehouse.Host/appsettings.json#L42)
- By default only the [root](https://github.com/Sholtee/warehouse/blob/730f3003f113bce393b899520cf610ed6c290845/SRC/App/Warehouse.Host/appsettings.json#L44) user can access the profiling results
- Every response from the API includes a header containing a tracking id `X-MiniProfiler-Ids` which can be used to query specific profiling data (`<base_url>/profiler/results?id=...`)

## Running the tests
Simply run the `.\Run-Tests.ps1` script. It places the tests result and coverage report to the `.\Artifacts` folder
  
## Deploying the app to AWS
- `.\CloudFormation\Deploy-Foundation.ps1 -action [create|update] -prefix [dev|test|prod] -region region-name -profile profile-name -certificate cert.crt -privateKey private.key`
- `.\CloudFormation\Deploy-Migrator.ps1 -action [create|update] -prefix [dev|test|prod] -region region-name -profile profile-name [-runMigrations]`
- `.\CloudFormation\Deploy-App.ps1 -action [create|update] -prefix [dev|test|prod] -region region-name -profile profile-name [-skipImageUpdate]`

### Scaling
- The server scales horizontally between [1](https://github.com/Sholtee/warehouse/blob/6f03102056d35dc1a64779a05a3f9e975cd74425/CloudFormation/app.dev.json#L4 ) and [2](https://github.com/Sholtee/warehouse/blob/6f03102056d35dc1a64779a05a3f9e975cd74425/CloudFormation/app.dev.json#L5 ) instances 
- The database scales vertically between [0.5](https://github.com/Sholtee/warehouse/blob/6f03102056d35dc1a64779a05a3f9e975cd74425/CloudFormation/foundation.dev.json#L3 ) and [1](https://github.com/Sholtee/warehouse/blob/6f03102056d35dc1a64779a05a3f9e975cd74425/CloudFormation/foundation.dev.json#L4 ) [ACU](https://docs.aws.amazon.com/AmazonRDS/latest/AuroraUserGuide/aurora-serverless-v2.how-it-works.html#aurora-serverless-v2.how-it-works.capacity )s