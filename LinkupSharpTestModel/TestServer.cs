using LinkupSharp;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace LinkupSharpTestModel
{
    public class TestServer : LinkupServer
    {
        public TestServer(string endpoint, X509Certificate2 certificate = null)
            : this(new string[] { endpoint }, certificate)
        {
        }

        public TestServer(IEnumerable<string> endpoints, X509Certificate2 certificate = null)
        {
            AddAuthenticator(new TestAuthenticator());
            AddModule(new TestServerModule());
            foreach (var endpoint in endpoints)
                AddListener(endpoint, certificate);
        }
    }
}
