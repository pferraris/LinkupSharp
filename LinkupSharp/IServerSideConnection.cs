using System;
using LinkupSharp.Channels;
using LinkupSharp.Security;
using LinkupSharp.Security.Authentication;

namespace LinkupSharp
{
    public interface IServerSideConnection : IDisposable
    {
        IChannel Channel { get; }
        Session Session { get; }
        Id Id { get; }
        bool IsConnected { get; }
        bool IsSignedIn { get; }

        event EventHandler<SignInEventArgs> SignInRequired;
        event EventHandler<SessionEventArgs> RestoreSessionRequired;
        event EventHandler<SessionEventArgs> SignOutRequired;
        event EventHandler<DisconnectedEventArgs> Disconnected;
        event EventHandler<PacketEventArgs> PacketReceived;

        void Connect(IChannel channel);
        void Disconnect();
        bool SetSession(Session session);
        bool CloseSession(Session session);
        void Send(object content);
        void Send(Packet packet);
    }
}