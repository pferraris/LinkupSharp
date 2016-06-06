#region License
/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2015 Pablo Ferraris
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion License

using LinkupSharp.Channels;
using LinkupSharp.Modules;
using LinkupSharp.Security;
using LinkupSharp.Security.Authentication;
using LinkupSharp.Serializers;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace LinkupSharp
{
    public class ClientConnection
    {
        internal IClientChannel Channel { get; private set; }
        public Session Session { get; private set; }
        public Id Id { get { return Session != null ? Session.Id : null; } }
        public bool IsConnected { get; private set; }
        public bool IsAuthenticated { get { return Session != null; } }

        private Disconnected disconnected;
        private bool serverSide;

        public ClientConnection()
        {
            IsConnected = false;
            sessionModule = new SessionModule();
            Session = null;
            modules = new List<IClientModule>();
        }

        #region Modules

        private SessionModule sessionModule;
        private List<IClientModule> modules;

        public IEnumerable<IClientModule> Modules { get { return modules.ToArray(); } }

        public void AddModule(IClientModule module)
        {
            if (module == null) throw new ArgumentNullException("Module cannot be null.");
            if (!modules.Contains(module))
            {
                modules.Add(module);
                module.OnAdded(this);
            }
        }

        public void RemoveModule(IClientModule module)
        {
            if (module == null) throw new ArgumentNullException("Module cannot be null.");
            if (modules.Contains(module))
            {
                modules.Remove(module);
                module.OnRemoved(this);
            }
        }

        #endregion Modules

        #region Channel

        private void Channel_PacketReceived(object sender, PacketEventArgs e)
        {
            if (sessionModule.Process(e.Packet, this))
                return;

            foreach (var module in Modules)
                if (module.Process(e.Packet, this))
                    return;

            OnPacketReceived(e);
        }

        void Channel_Closed(object sender, EventArgs e)
        {
            if (disconnected == null)
                disconnected = new Disconnected(Reasons.ConnectionLost);
            OnDisconnected(disconnected);
        }

        #endregion Channel

        #region Authentication

        public void SignIn(Id id)
        {
            SignIn(new SignIn(id));
        }

        public void SignIn(SignIn signIn)
        {
            serverSide = false;
            Send(signIn);
        }

        public void SignOut(Session session)
        {
            Send(new SignOut(session));
        }

        public void RestoreSession(Session session)
        {
            Send(new RestoreSession(session));
        }

        internal void SendConnected()
        {
            IsConnected = true;
            Send(new Connected());
        }

        internal bool Authenticate(Session session)
        {
            if (session == null) return false;
            serverSide = true;
            Session = session;
            Send(new SignedIn(session));
            return true;
        }

        internal bool CloseSession(Session session)
        {
            if (session.Id == Id)
            {
                if (session.Token == Session.Token)
                {
                    Session = null;
                    Send(new SignedOut(session, true));
                }
                else
                    Send(new SignedOut(session, false));
                return true;
            }
            return false;
        }

        #endregion Authentication

        #region Methods

        public void Send(object content)
        {
            if (content is Packet)
                Send(content as Packet);
            else
                Send(new Packet(content));
        }

        public void Send(Packet packet)
        {
            if (Channel != null)
            {
                if ((!serverSide) && (Session != null))
                    packet.Sender = Session.Id;
                Channel.Send(packet);
            }
        }

        public void Connect(string endpoint, X509Certificate2 certificate = null)
        {
            Connect<JsonPacketSerializer>(endpoint, certificate);
        }

        public void Connect<T>(string endpoint, X509Certificate2 certificate = null) where T : IPacketSerializer, new()
        {
            var uri = new Uri(endpoint);
            switch (uri.Scheme.ToLower())
            {
                case "tcp":
                case "ssl":
                    Connect(new TcpClientChannel<T>(uri.Host, uri.Port, certificate));
                    break;
                case "http":
                case "https":
                    Connect(new WebClientChannel<T>(uri.AbsoluteUri, certificate));
                    break;
                case "ws":
                case "wss":
                    Connect(new WebSocketClientChannel<T>(uri.AbsoluteUri, certificate));
                    break;
            }
        }

        public void Connect(IClientChannel channel)
        {
            if (Channel == null)
            {
                Channel = channel;
                Channel.PacketReceived += Channel_PacketReceived;
                Channel.Closed += Channel_Closed;
                Channel.Open().Wait();
            }
        }

        public void Disconnect()
        {
            Disconnect(Reasons.ClientRequest);
        }

        internal void Disconnect(Reasons reason, bool sendDisconnected = true)
        {
            if (Channel != null)
            {
                disconnected = new Disconnected(reason);
                if (sendDisconnected) Send(disconnected);
                Channel.Close();
            }
        }

        #endregion Methods

        #region Events

        internal event EventHandler<SignInEventArgs> SignInRequired;
        internal event EventHandler<SessionEventArgs> SignOutRequired;
        internal event EventHandler<SessionEventArgs> RestoreSessionRequired;
        public event EventHandler<EventArgs> Connected;
        public event EventHandler<EventArgs> SignedIn;
        public event EventHandler<EventArgs> SignedOut;
        public event EventHandler<EventArgs> AuthenticationFailed;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<PacketEventArgs> PacketReceived;

        protected internal virtual void OnDisconnected(Disconnected disconnected)
        {
            Channel.PacketReceived -= Channel_PacketReceived;
            Channel.Closed -= Channel_Closed;
            Channel = null;
            IsConnected = false;
            Disconnected?.Invoke(this, new DisconnectedEventArgs(disconnected));
        }

        protected internal virtual void OnConnected()
        {
            IsConnected = true;
            Connected?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnSignInRequired(SignIn signIn)
        {
            SignInRequired?.Invoke(this, new SignInEventArgs(signIn));
        }

        protected internal virtual void OnSignOutRequired(SignOut signOut)
        {
            SignOutRequired?.Invoke(this, new SessionEventArgs(signOut.Session));
        }

        protected internal void OnRestoreSessionRequired(Session session)
        {
            RestoreSessionRequired?.Invoke(this, new SessionEventArgs(session));
        }

        protected internal virtual void OnSignedIn(Session session)
        {
            Session = session;
            SignedIn?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnSignedOut(Session session, bool current)
        {
            if (current) Session = null;
            SignedOut?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnAuthenticationFailed()
        {
            AuthenticationFailed?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnPacketReceived(PacketEventArgs e)
        {
            PacketReceived?.Invoke(this, e);
        }

        #endregion Events

    }
}
