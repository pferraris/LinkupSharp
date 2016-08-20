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
        private bool HandleMessage(Packet packet, IServerSideConnection client, LinkupServer manager)
        {
            if ((packet.Recipient != null) && (manager.Connections.Any(x => x.Id == packet.Recipient)))
                foreach (var recipient in manager.Connections.Where(x => x.Id == packet.Recipient))
                    recipient.Send(packet);
            return true;
        }

        public override void OnAdded(LinkupServer manager)
        {
            base.OnAdded(manager);
            manager.ClientConnected += SendClients;
            manager.ClientDisconnected += SendClients;
        }

        public override void OnRemoved(LinkupServer manager)
        {
            base.OnRemoved(manager);
            manager.ClientConnected -= SendClients;
            manager.ClientDisconnected -= SendClients;
        }

        private void SendClients(object sender, ServerSideConnectionEventArgs e)
        {
            var clients = (sender as LinkupServer).Connections;
            foreach (var client in clients)
                client.Send(new Packet(clients.Select(x => x.Id).ToArray()) { Recipient = client.Id });
        }
    }

}