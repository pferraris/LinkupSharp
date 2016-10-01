using LinkupSharp;
using LinkupSharpTestModel;
using log4net.Config;
using System;
using System.Threading.Tasks;

namespace LinkupSharpDemo
{
    class Program
    {
        private static object syncLock = new object();

        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            var endpoints = new string[] { "ssl://+:5650/", "https://+:5651/", "wss://+:5652/" };

            using (var server = new TestServer(endpoints, Resources.CertificatePfx))
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                Console.WriteLine("Adding module to clients...");
                client1.AddModule(new TestClientModule());
                client2.AddModule(new TestClientModule());

                Console.WriteLine("Connecting clients...");
                client1.Connect("https://localhost:5651/", Resources.CertificateCer).Wait();
                client2.Connect("wss://localhost:5652/", Resources.CertificateCer).Wait();

                Console.WriteLine("Signing in clients...");
                client1.SignIn("client1@tests").Wait();
                client2.SignIn("client2@tests").Wait();

                Console.WriteLine("Sending greetings...");
                client1.Send(new Packet(new Message("Hello! client 2")) { Recipient = "client2@tests" } ).Wait();
                client2.Send(new Packet(new Message("Hello! client 1")) { Recipient = "client1@tests" }).Wait();
                Task.Delay(3000).Wait();

                Console.WriteLine("Disconnecting client1...");
                client1.Disconnect().Wait();

                Console.WriteLine("Reconnecting client1 & restore session...");
                client1.Connect("https://localhost:5651/", Resources.CertificateCer).Wait();
                client1.RestoreSession(client1.Session).Wait();

                Console.WriteLine("Sending goodbye...");
                client1.Send(new Packet(new Message("Bye! client 2")) { Recipient = "client2@tests" }).Wait();
                client2.Send(new Packet(new Message("Bye! client 1")) { Recipient = "client1@tests" }).Wait();
                Task.Delay(3000).Wait();

                Console.WriteLine("Disconnecting clients...");
                client1.Disconnect().Wait();
                client2.Disconnect().Wait();
            }

            Console.ReadLine();
        }
    }
}
