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
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Warehouse.Host.Services
{
    using DAL;

    internal sealed class RootUserRegistrar
    (
        IHostEnvironment env,
        ILogger<RootUserRegistrar> logger,
        IUserRepository userRepository,
        IPasswordHasher<string> passwordHasher,
        IAmazonSecretsManager secretsManager
    )
    {
        public async Task<bool> EnsureHasRootUserAsync()
        {
            const string ROOT_USER = "root";

            GetSecretValueResponse resp = await secretsManager.GetSecretValueAsync
            (
                new GetSecretValueRequest
                {
                    SecretId = $"{env.EnvironmentName}-warehouse-root-user-password"
                }
            );

            CreateUserParam createUserParam = new()
            {
                ClientId = ROOT_USER,
                ClientSecretHash = passwordHasher.HashPassword(ROOT_USER, resp.SecretString),
                Groups = ["Admins"]
            };

            if (!await userRepository.CreateUser(createUserParam))
                return false;
    
            logger.LogInformation("{root} user has been created.", ROOT_USER);
            return true;
        }

        public bool EnsureHasRootUser() => EnsureHasRootUserAsync().GetAwaiter().GetResult();
    }
}
