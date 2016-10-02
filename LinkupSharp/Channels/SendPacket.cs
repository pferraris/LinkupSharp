using System.Threading;

namespace LinkupSharp.Channels
{
    public class SendPacket
    {
        public Packet Packet { get; set; }
        public ManualResetEvent Sending { get; set; }
        public bool Sent { get; set; }
    }

}
