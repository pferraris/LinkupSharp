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
using LinkupSharp.Security.Authorization;
using LinkupSharp.Serializers;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LinkupSharp
{
    public class LinkupServer : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LinkupServer));

        private ISessionRepository sessions;
        private List<IServerSideConnection> connections;
        private List<IChannelListener> listeners;
        private List<IAuthenticator> authenticators;
        private List<IAuthorizer> authorizers;
        private List<IServerModule> modules;

        public ISessionRepository Sessions { get { return sessions; } }
        public IEnumerable<IServerSideConnection> Connections { get { lock (connections) return connections.Where(x => x.IsSignedIn).ToArray(); } }
        public IEnumerable<IServerSideConnection> Anonymous { get { lock (connections) return connections.Where(x => !x.IsSignedIn).ToArray(); } }
        public IEnumerable<IChannelListener> Listeners { get { return listeners.ToArray(); } }
        public IEnumerable<IAuthenticator> Authenticators { get { return authenticators.ToArray(); } }
        public IEnumerable<IAuthorizer> Authorizers { get { return authorizers.ToArray(); } }
        public IEnumerable<IServerModule> Modules { get { return modules.ToArray(); } }

        public LinkupServer()
        {
            sessions = new MemorySessionRepository();
            connections = new List<IServerSideConnection>();
            listeners = new List<IChannelListener>();
            authenticators = new List<IAuthenticator>();
            authorizers = new List<IAuthorizer>();
            modules = new List<IServerModule>();
        }

        public void Dispose()
        {
            ClearListeners();
            ClearAuthenticators();
            ClearModules();
            foreach (var connection in connections.ToArray())
                connection.Disconnect();
        }

        #region Listeners

        public void AddListener(IChannelListener listener)
        {
            if (listener == null) throw new ArgumentNullException("Listener cannot be null.");
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
                listener.ChannelConnected += listener_ChannelConnected;
                listener.Start();
            }
        }

        public void AddListener(string endpoint, X509Certificate2 certificate = null)
        {
            AddListener<JsonPacketSerializer>(endpoint, certificate);
        }

        public void AddListener<T>(string endpoint, X509Certificate2 certificate = null) where T : IPacketSerializer, new()
        {
            IChannelListener listener = null;
            var uri = new Uri(endpoint.Replace("+", "0.0.0.0"));
            switch (uri.Scheme.ToLower())
            {
                case "tcp":
                case "ssl":
                    listener = new TcpChannelListener();
                    break;
                case "http":
                case "https":
                    listener = new WebChannelListener();
                    break;
                case "ws":
                case "wss":
                    listener = new WebSocketChannelListener();
                    break;
            }
            if (listener != null)
            {
                if (new string[] { "ssl", "https", "wss" }.Contains(uri.Scheme.ToLower()))
                    listener.Certificate = certificate;
                listener.SetSerializer(new T());
                listener.Endpoint = endpoint;
                AddListener(listener);
            }
        }

        public void RemoveListener(IChannelListener listener)
        {
            if (listener == null) throw new ArgumentNullException("Listener cannot be null.");
            if (listeners.Contains(listener))
            {
                listener.ChannelConnected -= listener_ChannelConnected;
                listener.Stop();
                listeners.Remove(listener);
            }
        }

        public void ClearListeners()
        {
            foreach (var listener in Listeners)
                RemoveListener(listener);
        }

        #endregion Listeners

        #region Security

        public void AddAuthenticator(IAuthenticator authenticator)
        {
            if (authenticator == null) throw new ArgumentNullException("Authenticator cannot be null.");
            if (!authenticators.Contains(authenticator))
                authenticators.Add(authenticator);
        }

        public void RemoveAuthenticator(IAuthenticator authenticator)
        {
            if (authenticator == null) throw new ArgumentNullException("Authenticator cannot be null.");
            if (authenticators.Contains(authenticator))
                authenticators.Remove(authenticator);
        }

        public void ClearAuthenticators()
        {
            foreach (var authenticator in Authenticators)
                RemoveAuthenticator(authenticator);
        }

        public bool Authenticate(IServerSideConnection connection, SignIn signIn)
        {
            foreach (var authenticator in Authenticators)
                if (connection.SetSession(authenticator.Authenticate(signIn)))
                    return true;
            return false;
        }

        public void AddAuthorizer(IAuthorizer authorizer)
        {
            if (authorizer == null) throw new ArgumentNullException("Authorizer cannot be null.");
            if (!authorizers.Contains(authorizer))
                authorizers.Add(authorizer);
        }

        public void RemoveAuthorizer(IAuthorizer authorizer)
        {
            if (authorizer == null) throw new ArgumentNullException("Authorizer cannot be null.");
            if (authorizers.Contains(authorizer))
                authorizers.Remove(authorizer);
        }

        public void ClearAuthorizers()
        {
            foreach (var authorizer in Authorizers)
                RemoveAuthorizer(authorizer);
        }

        public bool IsAuthorized(IServerSideConnection connection, object[] roles)
        {
            if (!authorizers.Any()) return true;
            foreach (var authorizer in Authorizers)
                if (authorizer.IsAuthorized(connection.Session, roles))
                    return true;
            return false;
        }

        #endregion Security

        #region Modules

        public void AddModule(IServerModule module)
        {
            if (module == null) throw new ArgumentNullException("Module cannot be null.");
            if (!modules.Contains(module))
            {
                modules.Add(module);
                module.OnAdded(this);
            }
        }

        public void RemoveModule(IServerModule module)
        {
            if (module == null) throw new ArgumentNullException("Module cannot be null.");
            if (modules.Contains(module))
            {
                modules.Remove(module);
                module.OnRemoved(this);
            }
        }

        public void ClearModules()
        {
            foreach (var module in Modules)
                RemoveModule(module);
        }

        #endregion Modules

        #region Connections

        private void listener_ChannelConnected(object sender, ChannelEventArgs e)
        {
            IServerSideConnection connection = new ServerSideConnection();
            connection.SignInRequired += connection_SignInRequired;
            connection.SignOutRequired += connection_SignOutRequired;
            connection.RestoreSessionRequired += connection_RestoreSessionRequired;
            connection.PacketReceived += connection_PacketReceived;
            connection.Disconnected += connection_Disconnected;
            connection.Connect(e.Channel);
            if (!connections.Contains(connection))
                lock (connections)
                    connections.Add(connection);
        }

        void connection_Disconnected(object sender, DisconnectedEventArgs e)
        {
            var connection = sender as IServerSideConnection;
            connection.SignInRequired -= connection_SignInRequired;
            connection.SignOutRequired -= connection_SignOutRequired;
            connection.RestoreSessionRequired -= connection_RestoreSessionRequired;
            connection.PacketReceived -= connection_PacketReceived;
            connection.Disconnected -= connection_Disconnected;
            if (connections.Contains(connection))
                lock (connections)
                    connections.Remove(connection);
            if (connection.IsSignedIn)
                OnClientDisconnected(connection, connection.Id);
        }

        private void connection_SignInRequired(object sender, SignInEventArgs e)
        {
            var connection = sender as IServerSideConnection;
            if (connection.IsSignedIn) connection.CloseSession(connection.Session);
            if (Authenticate(connection, e.SignIn))
            {
                sessions.Add(connection.Session);
                OnClientConnected(connection, connection.Id);
                return;
            }
            connection.Send(new AuthenticationFailed(e.SignIn.Id));
        }

        private void connection_SignOutRequired(object sender, SessionEventArgs e)
        {
            var connection = sender as IServerSideConnection;
            if (connection.CloseSession(e.Session))
            {
                if (sessions.Contains(e.Session.Token))
                    sessions.Remove(e.Session);
                OnClientDisconnected(connection, e.Session.Id);
            }
        }

        void connection_RestoreSessionRequired(object sender, SessionEventArgs e)
        {
            var connection = sender as IServerSideConnection;
            if (sessions.Contains(e.Session.Token))
            {
                Session original = sessions.Get(e.Session.Token);
                if (original.Id == e.Session.Id)
                    if (connection.SetSession(original))
                    {
                        OnClientConnected(connection, connection.Id);
                        return;
                    }
            }
            connection.Send(new AuthenticationFailed(e.Session.Id));
        }

        private void connection_PacketReceived(object sender, PacketEventArgs e)
        {
            var connection = sender as IServerSideConnection;
            foreach (var module in Modules)
                if (module.Process(e.Packet, connection, this))
                    return;

            if (e.Packet.Recipient == null)
                Broadcast(e.Packet);
            else
                foreach (var other in Connections.Where(x => x.Id == e.Packet.Recipient))
                    other.Send(e.Packet);
        }

        private void Broadcast(Packet packet)
        {
            foreach (var connection in Connections)
                connection.Send(packet);
        }

        #endregion Connections

        #region Events

        public event EventHandler<ServerSideConnectionEventArgs> ClientConnected;
        public event EventHandler<ServerSideConnectionEventArgs> ClientDisconnected;

        protected virtual void OnClientConnected(IServerSideConnection connection, Id id)
        {
            ClientConnected?.Invoke(this, new ServerSideConnectionEventArgs(connection, id));
        }

        protected virtual void OnClientDisconnected(IServerSideConnection connection, Id id)
        {
            ClientDisconnected?.Invoke(this, new ServerSideConnectionEventArgs(connection, id));
        }

        #endregion Events

    }
}
