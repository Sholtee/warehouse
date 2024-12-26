/********************************************************************************
* RootUserRegistrar.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Warehouse.Host.Services
{
    using Core.Abstractions;
    using Core.Extensions;
    using DAL;

    internal sealed class RootUserRegistrar
    (
        IConfiguration configuration,
        ILogger<RootUserRegistrar> logger,
        IUserRepository userRepository,
        IPasswordGenerator passwordGenerator,
        IPasswordHasher<string> passwordHasher,
        IAmazonSecretsManager secretsManager
    )
    {
        public async Task<bool> EnsureHasRootUserAsync()
        {
            const string ROOT_USER = "root";

            string pw = passwordGenerator.Generate(20);

            CreateUserParam createUserParam = new()
            {
                ClientId = ROOT_USER,
                ClientSecretHash = passwordHasher.HashPassword(ROOT_USER, pw),
                Groups = ["Admins"]
            };

            if (!await userRepository.CreateUser(createUserParam))
                return false;

            string secret = $"{configuration.GetRequiredValue<string>("ASPNETCORE_ENVIRONMENT")}-warehouse-root-user-creds";

            await secretsManager.CreateSecretAsync
            (
                new CreateSecretRequest
                {
                    Name = secret,
                    SecretString = pw
                }
            );        

            logger.LogInformation("{root} user has been created. Change the password ASAP! Initial creds stored in {secret}", ROOT_USER, secret);
            return true;
        }

        public bool EnsureHasRootUser() => EnsureHasRootUserAsync().GetAwaiter().GetResult();
    }
}
