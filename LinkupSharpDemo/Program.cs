﻿using LinkupSharp;
using LinkupSharp.Authentication;
using LinkupSharp.Channels;
using LinkupSharp.Modules;
using System;
using System.Linq;

namespace LinkupSharpDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new TestServer();
            server.AddListener(new TcpChannelListener(5656));
            server.AddListener(new WebChannelListener("http://+:5657/"));
            server.AddListener(new WebSocketChannelListener("http://+:5658/"));

            var client1 = new TestClient();
            client1.Connected += (sender, e) => (sender as TestClient).Authenticate("client1@test");
            client1.Authenticated += client1_Authenticated;
            client1.Connect(CreateChannel());
            Console.ReadLine();
        }

        private static IClientChannel CreateChannel()
        {
            return new TcpClientChannel("localhost", 5656);
            //return new WebClientChannel("http://localhost:5657/");
            //return new WebSocketClientChannel("ws://localhost:5658/");
        }

        private static void client1_Authenticated(object sender, EventArgs e)
        {
            var client2 = new TestClient();
            client2.Connected += (senderInt, eInt) => (senderInt as TestClient).Authenticate("client2@test");
            client2.Authenticated += client2_Authenticated;
            client2.Connect(CreateChannel());
        }

        private static void client2_Authenticated(object sender, EventArgs e)
        {
            (sender as TestClient).SendMessage("Hi client1!!", "client1@test");
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
            Console.WriteLine("{0} => {1}", client.Id, String.Join<Id>("; ", packet.GetContent<Id[]>()));
            return true;
        }

        private bool HandleMessage(Packet packet, ClientConnection client)
        {
            Console.WriteLine("{0} => {1}: {2}", client.Id, packet.Sender, packet.GetContent<Message>().Text);
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