﻿#region License
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
    public class ConnectionManager : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ConnectionManager));

        private ISessionRepository sessions;
        private List<IServerSideClientConnection> clients;
        private List<IChannelListener> listeners;
        private List<IAuthenticator> authenticators;
        private List<IAuthorizer> authorizers;
        private List<IServerModule> modules;

        public ISessionRepository Sessions { get { return sessions; } }
        public IEnumerable<IServerSideClientConnection> Clients { get { lock (clients) return clients.Where(x => x.IsSignedIn).ToArray(); } }
        public IEnumerable<IServerSideClientConnection> Anonymous { get { lock (clients) return clients.Where(x => !x.IsSignedIn).ToArray(); } }
        public IEnumerable<IChannelListener> Listeners { get { return listeners.ToArray(); } }
        public IEnumerable<IAuthenticator> Authenticators { get { return authenticators.ToArray(); } }
        public IEnumerable<IAuthorizer> Authorizers { get { return authorizers.ToArray(); } }
        public IEnumerable<IServerModule> Modules { get { return modules.ToArray(); } }

        public ConnectionManager()
        {
            sessions = new MemorySessionRepository();
            clients = new List<IServerSideClientConnection>();
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
            foreach (var client in clients.ToArray())
                client.Disconnect();
        }

        #region Listeners

        public void AddListener(IChannelListener listener)
        {
            if (listener == null) throw new ArgumentNullException("Listener cannot be null.");
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
                listener.ClientConnected += listener_ClientConnected;
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
                listener.ClientConnected -= listener_ClientConnected;
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

        public bool Authenticate(IServerSideClientConnection client, SignIn signIn)
        {
            foreach (var authenticator in Authenticators)
                if (client.SetSession(authenticator.Authenticate(signIn)))
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

        public bool IsAuthorized(IServerSideClientConnection client, object[] roles)
        {
            if (!authorizers.Any()) return true;
            foreach (var authorizer in Authorizers)
                if (authorizer.IsAuthorized(client.Session, roles))
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

        #region Clients

        private void listener_ClientConnected(object sender, ClientChannelEventArgs e)
        {
            IServerSideClientConnection client = new ServerSideClientConnection();
            client.SignInRequired += client_SignInRequired;
            client.SignOutRequired += client_SignOutRequired;
            client.RestoreSessionRequired += client_RestoreSessionRequired;
            client.PacketReceived += client_PacketReceived;
            client.Disconnected += client_Disconnected;
            client.Connect(e.ClientChannel);
            if (!clients.Contains(client))
                lock (clients)
                    clients.Add(client);
        }

        void client_Disconnected(object sender, DisconnectedEventArgs e)
        {
            var client = sender as IServerSideClientConnection;
            client.SignInRequired -= client_SignInRequired;
            client.SignOutRequired -= client_SignOutRequired;
            client.RestoreSessionRequired -= client_RestoreSessionRequired;
            client.PacketReceived -= client_PacketReceived;
            client.Disconnected -= client_Disconnected;
            if (clients.Contains(client))
                lock (clients)
                    clients.Remove(client);
            if (client.IsSignedIn)
                OnClientDisconnected(client, client.Id);
        }

        private void client_SignInRequired(object sender, SignInEventArgs e)
        {
            var client = sender as IServerSideClientConnection;
            if (client.IsSignedIn) client.CloseSession(client.Session);
            if (Authenticate(client, e.SignIn))
            {
                sessions.Add(client.Session);
                OnClientConnected(client, client.Id);
                return;
            }
            client.Send(new AuthenticationFailed(e.SignIn.Id));
        }

        private void client_SignOutRequired(object sender, SessionEventArgs e)
        {
            var client = sender as IServerSideClientConnection;
            if (client.CloseSession(e.Session))
            {
                if (sessions.Contains(e.Session.Token))
                    sessions.Remove(e.Session);
                OnClientDisconnected(client, e.Session.Id);
            }
        }

        void client_RestoreSessionRequired(object sender, SessionEventArgs e)
        {
            var client = sender as IServerSideClientConnection;
            if (sessions.Contains(e.Session.Token))
            {
                Session original = sessions.Get(e.Session.Token);
                if (original.Id == e.Session.Id)
                    if (client.SetSession(original))
                    {
                        OnClientConnected(client, client.Id);
                        return;
                    }
            }
            client.Send(new AuthenticationFailed(e.Session.Id));
        }

        private void client_PacketReceived(object sender, PacketEventArgs e)
        {
            var client = sender as IServerSideClientConnection;
            foreach (var module in Modules)
                if (module.Process(e.Packet, client, this))
                    return;

            if (e.Packet.Recipient == null)
                Broadcast(e.Packet);
            else
                foreach (var other in Clients.Where(x => x.Id == e.Packet.Recipient))
                    other.Send(e.Packet);
        }

        private void Broadcast(Packet packet)
        {
            foreach (var client in Clients)
                client.Send(packet);
        }

        #endregion Clients

        #region Events

        public event EventHandler<ServerSideClientConnectionEventArgs> ClientConnected;
        public event EventHandler<ServerSideClientConnectionEventArgs> ClientDisconnected;

        protected virtual void OnClientConnected(IServerSideClientConnection client, Id id)
        {
            ClientConnected?.Invoke(this, new ServerSideClientConnectionEventArgs(client, id));
        }

        protected virtual void OnClientDisconnected(IServerSideClientConnection client, Id id)
        {
            ClientDisconnected?.Invoke(this, new ServerSideClientConnectionEventArgs(client, id));
        }

        #endregion Events

    }
}
