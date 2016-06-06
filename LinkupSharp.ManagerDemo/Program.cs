using LinkupSharp.Management;
using LinkupSharp.Security.Authentication;
using System;
using System.Configuration;

namespace LinkupSharp.ManagerDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var endpoint = Environment.GetEnvironmentVariable("LINKUP_MANAGEMENT_ENDPOINT");
            endpoint = endpoint ?? ConfigurationManager.AppSettings["LinkupManagementEndpoint"];
            endpoint = endpoint ?? "http://localhost:5465";

            using (var server = new ConnectionManager())
            {
                server.AddAuthenticator(new AnonymousAuthenticator());
                server.AddModule(new LinkupManagementModule(endpoint));
                server.AddListener("tcp://localhost:5466/");

                using (var client = new SyncClientConnection())
                {
                    client.Connect("tcp://localhost:5466/").Wait();
                    client.SignIn("pablo@fertex.com.ar").Wait();

                    Console.WriteLine("Press enter to end...");
                    Console.ReadLine();
                }
            }
        }
    }
}
