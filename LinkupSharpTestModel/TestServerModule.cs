using LinkupSharp;
using LinkupSharp.Modules;
using LinkupSharp.Security.Authentication;
using System.Linq;

namespace LinkupSharpTestModel
{
    public class TestServerModule : ServerModule
    {
        public TestServerModule()
        {
            RegisterHandler<Message>(HandleMessage);
        }

        [Authenticated]
        private bool HandleMessage(Packet packet, ClientConnection client, ConnectionManager manager)
        {
            if ((packet.Recipient != null) && (manager.Clients.Any(x => x.Id == packet.Recipient)))
                foreach (var recipient in manager.Clients.Where(x => x.Id == packet.Recipient))
                    recipient.Send(packet);
            return true;
        }

        public override void OnAdded(ConnectionManager manager)
        {
            base.OnAdded(manager);
            manager.ClientConnected += SendClients;
            manager.ClientDisconnected += SendClients;
        }

        public override void OnRemoved(ConnectionManager manager)
        {
            base.OnRemoved(manager);
            manager.ClientConnected -= SendClients;
            manager.ClientDisconnected -= SendClients;
        }

        private void SendClients(object sender, ClientConnectionEventArgs e)
        {
            var clients = (sender as ConnectionManager).Clients;
            foreach (var client in clients)
                client.Send(new Packet(clients.Select(x => x.Id).ToArray()) { Recipient = client.Id });
        }
    }

}