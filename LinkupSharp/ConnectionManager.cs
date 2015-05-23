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
using LinkupSharp.Loggers;
using LinkupSharp.Modules;
using LinkupSharp.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinkupSharp
{
    public class ConnectionManager : IDisposable
    {
        public TimeSpan AuthenticationTimeOut { get; set; }
        private Dictionary<ClientConnection, DateTime> pendings;
        private bool ckeckingAuthenticationTimeOut;
        private Task checkAuthentication;

        public TimeSpan InactivityTimeOut { get; set; }
        private Dictionary<ClientConnection, DateTime> disconnected;
        private bool ckeckingInactivityTimeOut;
        private Task checkInactivity;


        private Dictionary<string, ClientConnection> clients;
        public ReadOnlyDictionary<string, ClientConnection> Clients
        {
            get { return new ReadOnlyDictionary<string, ClientConnection>(clients); }
        }

        public ConnectionManager()
        {
            listeners = new List<IChannelListener>();
            authenticators = new List<IAuthenticator>();
            interceptors = new List<IServerModule>();

            clients = new Dictionary<string, ClientConnection>();
            pendings = new Dictionary<ClientConnection, DateTime>();
            disconnected = new Dictionary<ClientConnection, DateTime>();

            AuthenticationTimeOut = Settings.Default.AuthenticationTimeOut;
            if (AuthenticationTimeOut.TotalMilliseconds > 0)
            {
                ckeckingAuthenticationTimeOut = true;
                checkAuthentication = Task.Factory.StartNew(CheckAuthentication);
            }

            InactivityTimeOut = Settings.Default.InactivityTimeOut;
            if (InactivityTimeOut.TotalMilliseconds > 0)
            {
                ckeckingInactivityTimeOut = true;
                checkInactivity = Task.Factory.StartNew(CheckInactivity);
            }
        }

        public void Dispose()
        {
            ClearListeners();
            ClearAuthenticators();
            ClearInterceptors();
            ckeckingAuthenticationTimeOut = false;
            checkAuthentication.Wait();
            checkAuthentication.Dispose();
            foreach (var item in pendings.Keys.ToArray())
                item.Disconnect();
            ckeckingInactivityTimeOut = false;
            checkInactivity.Wait();
            checkInactivity.Dispose();
            foreach (var client in clients.Values.ToArray())
                client.Disconnect();
        }

        #region Listeners

        private List<IChannelListener> listeners;
        public ReadOnlyCollection<IChannelListener> Listeners
        {
            get { return listeners.AsReadOnly(); }
        }

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
            foreach (var listener in Listeners.ToArray())
                RemoveListener(listener);
        }

        #endregion Listeners

        #region Authenticators

        private List<IAuthenticator> authenticators;
        public ReadOnlyCollection<IAuthenticator> Authenticators
        {
            get { return authenticators.AsReadOnly(); }
        }

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
            foreach (var authenticator in Authenticators.ToArray())
                RemoveAuthenticator(authenticator);
        }

        #endregion Authenticators

        #region Modules

        private List<IServerModule> interceptors;
        public ReadOnlyCollection<IServerModule> Interceptors
        {
            get { return interceptors.AsReadOnly(); }
        }

        public void AddInterceptor(IServerModule interceptor)
        {
            if (interceptor == null) throw new ArgumentNullException("Interceptor cannot be null.");
            if (!interceptors.Contains(interceptor))
            {
                interceptors.Add(interceptor);
                interceptor.OnAdded(this);
            }
        }

        public void RemoveInterceptor(IServerModule interceptor)
        {
            if (interceptor == null) throw new ArgumentNullException("Interceptor cannot be null.");
            if (interceptors.Contains(interceptor))
                interceptors.Remove(interceptor);
        }

        public void ClearInterceptors()
        {
            foreach (var interceptor in Interceptors.ToArray())
                RemoveInterceptor(interceptor);
        }

        #endregion Modules

        #region Clients

        private void CheckAuthentication()
        {
            while (ckeckingAuthenticationTimeOut)
            {
                try
                {
                    lock (pendings)
                        while (pendings.Any(x => (DateTime.Now - x.Value) > AuthenticationTimeOut))
                        {
                            var item = pendings.First(x => (DateTime.Now - x.Value) > AuthenticationTimeOut);
                            item.Key.Disconnect(Reasons.AuthenticationTimeOut);
                            pendings.Remove(item.Key);
                        }
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error checking authentication timeout.");
                }
            }
        }

        private void CheckInactivity()
        {
            while (ckeckingInactivityTimeOut)
            {
                try
                {
                    lock (disconnected)
                        while (disconnected.Any(x => (DateTime.Now - x.Value) > InactivityTimeOut))
                        {
                            var item = disconnected.First(x => (DateTime.Now - x.Value) > InactivityTimeOut);
                            lock (clients)
                                if (clients.ContainsKey(item.Key.Id))
                                    clients.Remove(item.Key.Id);
                            disconnected.Remove(item.Key);
                            OnClientDisconnected(item.Key, item.Key.Id);
                        }
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error checking inactivity timeout.");
                }
            }
        }

        private void listener_ClientConnected(object sender, ClientChannelEventArgs e)
        {
            ClientConnection client = new ClientConnection();
            client.AuthenticationRequired += client_AuthenticationRequired;
            client.PacketReceived += client_PacketReceived;
            client.Disconnected += client_Disconnected;
            client.Connect(e.ClientChannel);
            lock (pendings)
                pendings.Add(client, DateTime.Now);
            client.RequireCredentials();
        }

        private void client_AuthenticationRequired(object sender, CredentialsEventArgs e)
        {
            ClientConnection client = sender as ClientConnection;
            ClientConnection oldClient = null;
            Id currentId = client.Id;
            foreach (var authenticator in Authenticators)
                if (client.Authenticate(authenticator.Authenticate(e.Credentials)))
                {
                    // Si se esta abriendo una sesión ya existente, pero con otra conexión. Debe desconectar la existente.
                    if ((clients.ContainsKey(client.Id)) && (clients[client.Id] != client))
                    {
                        oldClient = clients[client.Id];
                        if (disconnected.ContainsKey(oldClient))
                            disconnected.Remove(oldClient);
                        else
                            oldClient.Disconnect(Reasons.AnotherSessionOpened);
                        clients.Remove(client.Id);
                    }
                    // Si se trata de un cambio de usuario, debe quitar el Id viejo de la lista de clientes.
                    if ((currentId != null) && (!currentId.Equals(client.Id)))
                        if (clients.ContainsKey(currentId))
                        {
                            lock (clients)
                                clients.Remove(currentId);
                            OnClientDisconnected(client, currentId);
                        }
                    // En caso de no estar el Id en la lista de clientes lo agrega.
                    if (!clients.ContainsKey(client.Id))
                    {
                        lock (clients)
                            clients.Add(client.Id, client);
                        if (oldClient == null)
                            OnClientConnected(client, client.Id);
                        else
                            OnClientReconnected(client, client.Id);
                    }
                    // En caso de estar el cliente en la lista de pendientes, lo quita.
                    if (pendings.ContainsKey(client))
                        lock (pendings)
                            pendings.Remove(client);
                    return;
                }
            client.Send(new AuthenticationFailed(e.Credentials.Id));
        }

        private void client_PacketReceived(object sender, PacketEventArgs e)
        {
            foreach (var interceptor in Interceptors)
                if (interceptor.Process(e.Packet, sender as ClientConnection, this))
                    return;

            if (e.Packet.Recipient == null)
                Broadcast(e.Packet);
            else if (clients.ContainsKey(e.Packet.Recipient))
                clients[e.Packet.Recipient].Send(e.Packet);
        }

        void client_Disconnected(object sender, DisconnectedEventArgs e)
        {
            ClientConnection client = sender as ClientConnection;
            client.AuthenticationRequired -= client_AuthenticationRequired;
            client.PacketReceived -= client_PacketReceived;
            client.Disconnected -= client_Disconnected;
            switch (e.Disconnected.Reason)
            {
                case Reasons.AnotherSessionOpened:
                    break;
                case Reasons.AuthenticationTimeOut:
                    if (pendings.ContainsKey(client))
                        lock (pendings)
                            pendings.Remove(client);
                    break;
                default:
                    if ((clients.ContainsKey(client.Id)) && (!disconnected.ContainsKey(client)))
                    {
                        lock (disconnected)
                            disconnected.Add(client, DateTime.Now);
                        OnClientInactive(client, client.Id);
                    }
                    break;
            }
        }

        private void Broadcast(Packet packet)
        {
            foreach (var client in clients.Values)
                if (!client.Id.Equals(packet.Sender))
                    client.Send(packet);
        }

        #endregion Clients

        #region Events

        public event EventHandler<ClientConnectionEventArgs> ClientConnected;
        public event EventHandler<ClientConnectionEventArgs> ClientInactive;
        public event EventHandler<ClientConnectionEventArgs> ClientReconnected;
        public event EventHandler<ClientConnectionEventArgs> ClientDisconnected;

        protected virtual void OnClientConnected(ClientConnection client, Id id)
        {
            if (ClientConnected != null)
                ClientConnected(this, new ClientConnectionEventArgs(client, id));
        }

        protected virtual void OnClientInactive(ClientConnection client, Id id)
        {
            if (ClientInactive != null)
                ClientInactive(this, new ClientConnectionEventArgs(client, id));
        }

        protected virtual void OnClientReconnected(ClientConnection client, Id id)
        {
            if (ClientReconnected != null)
                ClientReconnected(this, new ClientConnectionEventArgs(client, id));
        }

        protected virtual void OnClientDisconnected(ClientConnection client, Id id)
        {
            if (ClientDisconnected != null)
                ClientDisconnected(this, new ClientConnectionEventArgs(client, id));
        }

        #endregion Events

    }
}
