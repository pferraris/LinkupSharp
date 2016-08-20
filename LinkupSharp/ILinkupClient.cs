using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using LinkupSharp.Channels;
using LinkupSharp.Modules;
using LinkupSharp.Security;
using LinkupSharp.Security.Authentication;
using LinkupSharp.Serializers;
using System.Threading.Tasks;

namespace LinkupSharp
{
    public interface ILinkupClient : IDisposable
    {
        IChannel Channel { get; }
        Session Session { get; }
        Id Id { get; }
        bool IsConnected { get; }
        bool IsSignedIn { get; }
        IEnumerable<IClientModule> Modules { get; }

        event EventHandler<EventArgs> Connected;
        event EventHandler<DisconnectedEventArgs> Disconnected;
        event EventHandler<EventArgs> SignedIn;
        event EventHandler<EventArgs> AuthenticationFailed;
        event EventHandler<EventArgs> SignedOut;
        event EventHandler<PacketEventArgs> PacketReceived;

        void AddModule(IClientModule module);
        void RemoveModule(IClientModule module);

        Task<bool> Connect(string endpoint, X509Certificate2 certificate = null);
        Task<bool> Connect<T>(string endpoint, X509Certificate2 certificate = null) where T : IPacketSerializer, new();
        Task<bool> Connect(IChannel channel);
        Task<bool> Disconnect();
        Task<bool> SignIn(Id id);
        Task<bool> SignIn(SignIn signIn);
        Task<bool> RestoreSession(Session session);
        Task<bool> SignOut(Session session);
        Task<bool> Send(object content);
        Task<bool> Send(Packet packet);
    }
}