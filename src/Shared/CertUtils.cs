using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Shared;

public static class CertUtils
{
    public static bool Validate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        const SslPolicyErrors ignoredErrors =
            SslPolicyErrors.RemoteCertificateChainErrors |  // self-signed
            SslPolicyErrors.RemoteCertificateNameMismatch;  // name mismatch

        if ((sslPolicyErrors & ~ignoredErrors) == SslPolicyErrors.None)
        {
            return true;
        }
        return false;
    }

    public static X509CertificateCollection LoadCerts(string path, string passphrase)
    {
        string certPem = File.ReadAllText(Path.Combine(path, "tls.crt"));
        string keyPem = File.ReadAllText(Path.Combine(path, "tls.key"));

        X509Certificate2 clientCert = X509Certificate2.CreateFromPem(certPem, keyPem);
        clientCert = new X509Certificate2(clientCert.Export(X509ContentType.Pfx));

        return new X509Certificate2Collection { clientCert };
    }
}
