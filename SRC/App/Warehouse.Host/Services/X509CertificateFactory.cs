/********************************************************************************
* X509CertificateFactory.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Security.Cryptography.X509Certificates;

namespace Warehouse.Host.Services
{
    using Core.Abstractions;

    internal sealed class X509CertificateFactory : IX509CertificateFactory
    {
        public X509Certificate2 CreateFromPem(string certificate, string privateKey) =>
            X509Certificate2.CreateFromPem(certificate, privateKey);
    }
}
