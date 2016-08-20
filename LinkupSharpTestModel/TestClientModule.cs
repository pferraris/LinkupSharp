using LinkupSharp;
using LinkupSharp.Modules;
using System;

namespace LinkupSharpTestModel
{
    public class TestClientModule : ClientModule
    {
        public TestClientModule()
        {
            RegisterHandler<Id[]>(HandleContacts);
            RegisterHandler<Message>(HandleMessage);
        }

        private bool HandleContacts(Packet packet, ILinkupClient client)
        {
            Console.WriteLine($"{client.Id} => Contacts: {string.Join<Id>("; ", packet.GetContent<Id[]>())}");
            return true;
        }

        private bool HandleMessage(Packet packet, ILinkupClient client)
        {
            Console.WriteLine($"{client.Id} => {packet.GetContent<Message>().Text}");
            return true;
        }
    }

}
