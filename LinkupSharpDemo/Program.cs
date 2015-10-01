using LinkupSharp;
using LinkupSharp.Authentication;
using LinkupSharp.Modules;
using log4net.Config;
using System;
using System.Linq;

namespace LinkupSharpDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            var server = new TestServer();
            server.AddListener("tcp://0.0.0.0:5656/");
            server.AddListener("http://0.0.0.0:5657/");
            server.AddListener("ws://0.0.0.0:5658/");

            var client1 = new TestClient();
            client1.Connected += (sender, e) => client1.Authenticate("client1@test");
            client1.Authenticated += client1_Authenticated;
            client1.Connect(GetEndpoint());
            Console.ReadLine();
        }

        private static string GetEndpoint()
        {
            //return "tcp://localhost:5656/";
            //return "http://localhost:5657/";
            return "ws://localhost:5658/";
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
                    client2.Connect(GetEndpoint());
                }
            };

            client2.Connect(GetEndpoint());
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
        public string Text { get; set; }

        public Message(string text)
        {
            Text = text;
        }
    }
}
