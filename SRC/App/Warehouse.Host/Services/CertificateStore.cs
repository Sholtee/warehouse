/********************************************************************************
* CertificateStore.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;


namespace Warehouse.Host.Services
{
    using Core.Extensions;

    internal sealed class CertificateStore(IConfiguration configuration, IAmazonSecretsManager secretsManager, IOptions<JsonOptions> jsonOpts)
    {
        private sealed record Pem(string PrivateKey, string Certificate);

        internal Func<string, string, X509Certificate2> CreateFromPem { get; init; } = static (cert, privateKey) => // to be mocked
            X509Certificate2.CreateFromPem(cert, privateKey);

        public async Task<X509Certificate2> GetCertificateAsync(string name)
        {
            GetSecretValueResponse resp = await secretsManager.GetSecretValueAsync
            (
                new GetSecretValueRequest
                {
                    SecretId = $"{configuration.GetRequiredValue<string>("ASPNETCORE_ENVIRONMENT")}-{name}"
                }
            );

            Pem pem = JsonSerializer.Deserialize<Pem>(resp.SecretString, jsonOpts.Value.JsonSerializerOptions)!;

            return CreateFromPem(pem.Certificate, pem.PrivateKey);
        }

        public X509Certificate2 GetCertificate(string name) => GetCertificateAsync(name).GetAwaiter().GetResult();
    }
}
