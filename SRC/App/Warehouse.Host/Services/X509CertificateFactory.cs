using System.Security.Cryptography.X509Certificates;

namespace Warehouse.API.Services
{
    /// <summary>
    /// Contract on how to create certificates.
    /// </summary>
    public interface IX509CertificateFactory
    {
        /// <summary>
        /// Creates a new <see cref="X509Certificate"/> from the given <paramref name="certificate"/> and <paramref name="privateKey"/>
        /// </summary>
        X509Certificate2 CreateFromPem(string certificate, string privateKey);
    }

    internal sealed class X509CertificateFactory : IX509CertificateFactory
    {
        public X509Certificate2 CreateFromPem(string certificate, string privateKey) =>
            X509Certificate2.CreateFromPem(certificate, privateKey);
    }
}
