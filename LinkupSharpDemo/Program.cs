﻿using LinkupSharp;
using LinkupSharp.Modules;
using LinkupSharp.Security.Authentication;
using LinkupSharp.Serializers;
using log4net.Config;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LinkupSharpDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            certificatePfx = new X509Certificate2(LoadResource("LinkupSharpDemo.Resources.certificate.pfx"), LoadResourceString("LinkupSharpDemo.Resources.certificate.key"));
            certificateCer = new X509Certificate2(LoadResource("LinkupSharpDemo.Resources.certificate.cer"));

            var server = new TestServer();
            server.AddListener("ssl://0.0.0.0:5650/", certificatePfx);
            server.AddListener("http://0.0.0.0:5651/", certificatePfx);
            server.AddListener("wss://localhost:5652/", certificatePfx);
            server.AddListener<ProtoPacketSerializer>("ssl://0.0.0.0:5653/", certificatePfx);
            server.AddListener<ProtoPacketSerializer>("http://0.0.0.0:5654/", certificatePfx);
            server.AddListener<ProtoPacketSerializer>("wss://localhost:5655/", certificatePfx);

            var client1 = new TestClient();
            client1.Connected += (sender, e) => client1.Authenticate("client1@test");
            client1.Authenticated += client1_Authenticated;
            Connect(client1, certificateCer);
            Console.ReadLine();
        }

        private static void Connect(ClientConnection client, X509Certificate2 certificate)
        {
            //client.Connect("ssl://localhost:5650/", certificate);
            //client.Connect("http://localhost:5651/", certificate);
            //client.Connect("wss://localhost:5652/", certificate);
            client.Connect<ProtoPacketSerializer>("ssl://localhost:5653/", certificate);
            //client.Connect<ProtoPacketSerializer>("http://localhost:5654/", certificate);
            //client.Connect<ProtoPacketSerializer>("wss://localhost:5655/", certificate);
        }

        private static X509Certificate2 certificatePfx;
        private static X509Certificate2 certificateCer;

        private static byte[] LoadResource(string resourceName)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                var buffer = new byte[8 * 1024];
                var len = stream.Read(buffer, 0, buffer.Length);
                return buffer.Take(len).ToArray();
            }
        }

        private static string LoadResourceString(string resourceName)
        {
            return Encoding.UTF8.GetString(LoadResource(resourceName));
        }

        private static void client1_Authenticated(object sender, EventArgs e)
        {
            var client2 = new TestClient();

            client2.Connected += (senderInt, eInt) =>
            {
                if (client2.SessionContext == null)
                    client2.Authenticate("client2@test");
                else
                    client2.Send((senderInt as TestClient).SessionContext);
            };

            client2.Authenticated += (senderInt, eInt) =>
            {
                client2.SendMessage("Hi client1!!", "client1@test");
            };

            bool reconnected = false;
            client2.Disconnected += (senderInt, eInt) =>
            {
                if (!reconnected)
                {
                    reconnected = true;
                    Connect(client2, certificateCer);
                }
            };

            Connect(client2, certificateCer);
        }
    }

    public class TestServer : ConnectionManager
    {
        public TestServer()
        {
            AddAuthenticator(new AnonymousAuthenticator());
            AddModule(new TestServerModule());
        }
    }

    public class TestClient : ClientConnection
    {
        public TestClient()
        {
            AddModule(new TestClientModule());
        }

        public void SendMessage(string text, Id recipient)
        {
            Send(new Packet(new Message(text)) { Recipient = recipient });
        }
    }

    public class TestServerModule : ServerModule
    {
        public TestServerModule()
        {
            RegisterHandler<Message>(HandleMessage);
        }

        [Authenticated]
        private bool HandleMessage(Packet packet, ClientConnection client, ConnectionManager manager)
        {
            if ((packet.Recipient != null) && (manager.Clients.ContainsKey(packet.Recipient)))
            {
                manager.Clients[packet.Recipient].Send(packet);
                client.Send(packet);
            }
            return true;
        }

        public override void OnAdded(ConnectionManager manager)
        {
            base.OnAdded(manager);
            manager.ClientConnected += SendClients;
            manager.ClientDisconnected += SendClients;
        }

        private void SendClients(object sender, ClientConnectionEventArgs e)
        {
            var clients = (sender as ConnectionManager).Clients.Values.ToArray();
            foreach (var client in clients)
                client.Send(new Packet(clients.Select(x => x.Id).ToArray()) { Recipient = client.Id });
        }
    }

    public class TestClientModule : ClientModule
    {
        public TestClientModule()
        {
            RegisterHandler<Id[]>(HandleContacts);
            RegisterHandler<Message>(HandleMessage);
        }

        private bool HandleContacts(Packet packet, ClientConnection client)
        {
            Console.WriteLine("{0} => Contacts: {1}", client.Id, String.Join<Id>("; ", packet.GetContent<Id[]>()));
            return true;
        }

        private bool HandleMessage(Packet packet, ClientConnection client)
        {
            Console.WriteLine("{0} => {1} say: {2}", client.Id, packet.Sender, packet.GetContent<Message>().Text);
            client.Disconnect();
            return true;
        }
    }

    public class Message
    {
        public string Text { get; private set; }

        public Message(string text)
        {
            Text = text;
        }
    }
}
