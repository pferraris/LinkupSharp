using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using LinkupSharp.Channels;
using LinkupSharp.Modules;
using LinkupSharp.Security;
using LinkupSharp.Security.Authentication;
using LinkupSharp.Serializers;

namespace LinkupSharp
{
    public interface IClientConnection
    {
        IClientChannel Channel { get; }
        Session Session { get; }
        Id Id { get; }
        bool IsAuthenticated { get; }
        bool IsConnected { get; }
        IEnumerable<IClientModule> Modules { get; }

        event EventHandler<EventArgs> Connected;
        event EventHandler<DisconnectedEventArgs> Disconnected;
        event EventHandler<EventArgs> SignedIn;
        event EventHandler<EventArgs> AuthenticationFailed;
        event EventHandler<EventArgs> SignedOut;
        event EventHandler<PacketEventArgs> PacketReceived;

        void AddModule(IClientModule module);
        void RemoveModule(IClientModule module);
        void Connect(string endpoint, X509Certificate2 certificate = null);
        void Connect<T>(string endpoint, X509Certificate2 certificate = null) where T : IPacketSerializer, new();
        void Connect(IClientChannel channel);
        void Disconnect();
        void SignIn(Id id);
        void SignIn(SignIn signIn);
        void RestoreSession(Session session);
        void SignOut(Session session);
        void Send(object content);
        void Send(Packet packet);
    }
}