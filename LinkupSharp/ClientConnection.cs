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
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LinkupSharp
{
    public class ClientConnection : IClientConnection
    {
        public IClientChannel Channel { get; private set; }
        public Session Session { get; private set; }
        public Id Id { get { return Session != null ? Session.Id : null; } }
        public bool IsConnected { get; private set; }
        public bool IsAuthenticated { get { return Session != null; } }

        private Disconnected disconnected;

        public ClientConnection()
        {
            IsConnected = false;
            Session = null;
            modules = new List<IClientModule>();

            RegisterHandler<SignedIn>(packet =>
            {
                OnSignedIn(packet.GetContent<SignedIn>().Session);
                return true;
            });

            RegisterHandler<SignedOut>(packet =>
            {
                var signedOut = packet.GetContent<SignedOut>();
                OnSignedOut(signedOut.Session, signedOut.Current);
                return true;
            });

            RegisterHandler<AuthenticationFailed>(packet =>
            {
                OnAuthenticationFailed();
                return true;
            });

            RegisterHandler<Connected>(packet =>
            {
                OnConnected();
                return true;
            });

            RegisterHandler<Disconnected>(packet =>
            {
                Disconnect(packet.GetContent<Disconnected>().Reason, false);
                return true;
            });
        }

        #region Modules

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
            if (Process(e.Packet))
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
                if (Session != null)
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
            IClientChannel channel = null;
            var uri = new Uri(endpoint);
            switch (uri.Scheme.ToLower())
            {
                case "tcp":
                case "ssl":
                    channel = new TcpClientChannel();
                    break;
                case "http":
                case "https":
                    channel = new WebClientChannel();
                    break;
                case "ws":
                case "wss":
                    channel = new WebSocketClientChannel();
                    break;
            }
            if (channel != null)
            {
                if (new string[] { "ssl", "https", "wss" }.Contains(uri.Scheme.ToLower()))
                    channel.Certificate = certificate;
                channel.SetSerializer(new T());
                channel.Endpoint = endpoint;
                Connect(channel);
            }
        }

        public void Connect(IClientChannel channel)
        {
            if (Channel == null)
            {
                Channel = channel;
                Channel.PacketReceived += Channel_PacketReceived;
                Channel.Closed += Channel_Closed;
                try
                {
                    Channel.Open().Wait();
                }
                catch (Exception ex)
                {
                    Channel = null;
                    throw ex;
                }
            }
        }

        public void Disconnect()
        {
            Disconnect(Reasons.ClientRequest);
        }

        private void Disconnect(Reasons reason, bool sendDisconnected = true)
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

        #region Packet Handlers

        private delegate bool PacketHandler(Packet packet);
        private List<Tuple<Type, PacketHandler>> packetHandlers = new List<Tuple<Type, PacketHandler>>();

        private bool Process(Packet packet)
        {
            foreach (var handler in packetHandlers)
                if (packet.Is(handler.Item1))
                    if (handler.Item2(packet))
                        return true;
            return false;
        }

        private void RegisterHandler<T>(PacketHandler handler)
        {
            RegisterHandler(typeof(T), handler);
        }

        private void RegisterHandler(Type type, PacketHandler handler)
        {
            packetHandlers.Add(Tuple.Create(type, handler));
        }

        #endregion Packet Handlers

    }
}
