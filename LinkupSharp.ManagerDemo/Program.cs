using LinkupSharp.Management;
using LinkupSharp.Security.Authentication;
using System;
using System.Configuration;
using System.Linq;

namespace LinkupSharp.ManagerDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var endpoint = Environment.GetEnvironmentVariable("LINKUP_MANAGEMENT_ENDPOINT");
            endpoint = endpoint ?? ConfigurationManager.AppSettings["LinkupManagementEndpoint"];
            endpoint = endpoint ?? "http://localhost:5465";

            Packet.RegisterType<Message>();

            using (var server = new ConnectionManager())
            {
                server.AddAuthenticator(new AnonymousAuthenticator());
                server.AddModule(new LinkupManagementModule(endpoint));
                server.AddListener("tcp://localhost:5466/");

                using (var client = new SyncClientConnection())
                {
                    client.Client.PacketReceived += Client_PacketReceived;
                    client.Connect("tcp://localhost:5466/").Wait();
                    client.SignIn("pablo@fertex.com.ar").Wait();

                    Console.WriteLine($"Listening in {server.Listeners.First().Endpoint}. Press enter to end...");
                    Console.ReadLine();
                }
            }
        }

        private static void Client_PacketReceived(object sender, PacketEventArgs e)
        {
            Console.WriteLine($"{e.Packet} - {e.Packet.GetContent()}");
        }
    }
}
