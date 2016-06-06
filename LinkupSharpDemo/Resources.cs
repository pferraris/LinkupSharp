using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LinkupSharpDemo
{
    public static class Resources
    {
        private static X509Certificate2 certificatePfx = new X509Certificate2(LoadResource("LinkupSharpDemo.Resources.certificate.pfx"), LoadResourceString("LinkupSharpDemo.Resources.certificate.key"));
        private static X509Certificate2 certificateCer = new X509Certificate2(LoadResource("LinkupSharpDemo.Resources.certificate.cer"));

        public static X509Certificate2 CertificatePfx { get { return certificatePfx; } }
        public static X509Certificate2 CertificateCer { get { return certificateCer; } }

        public static byte[] LoadResource(string resourceName)
        {
            using (var stream = Assembly.GetEntryAssembly().GetManifestResourceStream(resourceName))
            {
                var buffer = new byte[8 * 1024];
                var len = stream.Read(buffer, 0, buffer.Length);
                return buffer.Take(len).ToArray();
            }
        }

        public static string LoadResourceString(string resourceName)
        {
            return Encoding.UTF8.GetString(LoadResource(resourceName));
        }
    }
}
