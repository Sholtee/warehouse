using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Warehouse.API.Services
{    
    using DAL;
    using Extensions;

    internal sealed class RootUserRegistrar
    (
        IConfiguration configuration,
        ILogger<RootUserRegistrar> logger,
        IUserRepository userRepository,
        IPasswordHasher<string> passwordHasher,
        IAmazonSecretsManager secretsManager
    )
    {
        private static string GeneratePassword()
        {
            const string 
                UPPER_CASE_LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                LOWER_CASE_LETTERS = "abcdefghijklmnopqrstuvwxyz",
                NUMBERS = "0123456789",
                SPECIALS = "!@#$%^&*()_+";

            List<char> chars = [];

            chars.AddRange(UPPER_CASE_LETTERS.Random(5));
            chars.AddRange(LOWER_CASE_LETTERS.Random(5));
            chars.AddRange(NUMBERS.Random(5));
            chars.AddRange(SPECIALS.Random(5));

            return string.Join("", chars.Shuffle());
        }

        public async Task<bool> EnsureHasRootUserAsync()
        {
            const string ROOT_USER = "root";

            string pw = GeneratePassword();

            CreateUserParam createUserParam = new()
            {
                ClientId = ROOT_USER,
                ClientSecretHash = passwordHasher.HashPassword(ROOT_USER, pw),
                Groups = ["Admins"]
            };

            if (!await userRepository.CreateUser(createUserParam))
                return false;

            string secret = $"{configuration.GetRequiredValue<string>("Prefix")}-root-user-creds";

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
