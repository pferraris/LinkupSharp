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

using LinkupSharp.Authentication;
using LinkupSharp.Channels;
using LinkupSharp.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LinkupSharp
{
    public class ClientConnection
    {
        internal IClientChannel Channel { get; private set; }
        public SessionContext SessionContext { get; private set; }
        public Id Id { get { return SessionContext != null ? SessionContext.Id : null; } }
        public bool IsConnected { get; private set; }
        public bool IsAuthenticated { get { return SessionContext != null; } }

        private Disconnected disconnected;
        private bool serverSide;

        public ClientConnection()
        {
            IsConnected = false;
            sessionModule = new SessionModule();
            SessionContext = null;
            modules = new List<IClientModule>();
        }

        #region Modules

        private SessionModule sessionModule;
        private List<IClientModule> modules;
        public ReadOnlyCollection<IClientModule> Modules
        {
            get { return modules.AsReadOnly(); }
        }

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
                modules.Remove(module);
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

        public void Authenticate(string username, string domain)
        {
            Authenticate(new Credentials(new Id(username, domain)));
        }

        public void Authenticate(string id)
        {
            Authenticate(new Credentials(id));
        }

        public void Authenticate(Id id)
        {
            Authenticate(new Credentials(id));
        }

        public void Authenticate(Credentials credentials)
        {
            serverSide = false;
            Send(credentials);
        }

        internal void RequireCredentials()
        {
            IsConnected = true;
            Send(new Connected());
        }

        internal bool Authenticate(SessionContext sessionContext)
        {
            if (sessionContext != null)
            {
                serverSide = true;
                SessionContext = sessionContext;
                Send(new Authenticated(sessionContext));
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
                if ((!serverSide) && (SessionContext != null))
                    packet.Sender = SessionContext.Id;
                Channel.Send(packet);
            }
        }

        public void Connect(IClientChannel channel)
        {
            Channel = channel;
            Channel.PacketReceived += Channel_PacketReceived;
            Channel.Closed += Channel_Closed;
        }

        public void Disconnect()
        {
            if (serverSide)
                Disconnect(Reasons.ServerRequest);
            else
                Disconnect(Reasons.ClientRequest);
        }

        internal void Disconnect(Reasons reason, bool sendDisconnected = true)
        {
            disconnected = new Disconnected(reason);
            if (Channel != null)
            {
                if (sendDisconnected) Send(disconnected);
                Channel.Close();
            }
        }

        #endregion Methods

        #region Events

        public event EventHandler<PacketEventArgs> PacketReceived;
        public event EventHandler<CredentialsEventArgs> AuthenticationRequired;
        public event EventHandler<SessionContextEventArgs> RestoreSessionRequired;
        public event EventHandler<EventArgs> Connected;
        public event EventHandler<EventArgs> Authenticated;
        public event EventHandler<EventArgs> AuthenticationFailed;
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        protected internal virtual void OnPacketReceived(PacketEventArgs e)
        {
            if (PacketReceived != null)
                PacketReceived(this, e);
        }

        protected internal virtual void OnDisconnected(Disconnected disconnected)
        {
            Channel.PacketReceived -= Channel_PacketReceived;
            Channel.Closed -= Channel_Closed;
            Channel = null;
            IsConnected = false;
            if (Disconnected != null)
                Disconnected(this, new DisconnectedEventArgs(disconnected));
        }

        protected internal virtual void OnConnected()
        {
            IsConnected = true;
            if (Connected != null)
                Connected(this, EventArgs.Empty);
        }

        protected internal virtual void OnAuthenticationRequired(Credentials credentials)
        {
            if (AuthenticationRequired != null)
                AuthenticationRequired(this, new CredentialsEventArgs(credentials));
        }

        protected internal void OnRestoreSessionRequired(SessionContext sessionContext)
        {
            if (RestoreSessionRequired != null)
                RestoreSessionRequired(this, new SessionContextEventArgs(sessionContext));
        }

        protected internal virtual void OnAuthenticated(SessionContext sessionContext)
        {
            SessionContext = sessionContext;
            if (Authenticated != null)
                Authenticated(this, EventArgs.Empty);
        }

        protected internal virtual void OnAuthenticationFailed()
        {
            if (AuthenticationFailed != null)
                AuthenticationFailed(this, EventArgs.Empty);
        }

        #endregion Events

    }
}
