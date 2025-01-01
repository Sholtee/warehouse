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