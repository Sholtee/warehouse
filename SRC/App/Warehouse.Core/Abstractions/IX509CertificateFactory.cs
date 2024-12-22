/********************************************************************************
* IX509CertificateFactory.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Security.Cryptography.X509Certificates;

namespace Warehouse.Core.Abstractions
{
    public interface IX509CertificateFactory
    {
        X509Certificate2 CreateFromPem(string certificate, string privateKey);
    }
}
