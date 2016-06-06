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
using System.Threading;
using System.Threading.Tasks;

namespace LinkupSharp
{
    public class SyncClientConnection : IDisposable
    {
        private ManualResetEvent connectEvent = new ManualResetEvent(false);
        private ManualResetEvent disconnectEvent = new ManualResetEvent(false);
        private ManualResetEvent signInEvent = new ManualResetEvent(false);
        private ManualResetEvent signOutEvent = new ManualResetEvent(false);
        private ManualResetEvent receiveEvent = new ManualResetEvent(false);

        private Queue<Packet> packets = new Queue<Packet>();

        public ClientConnection Client { get; private set; }

        public Session Session { get { return Client.Session; } }
        public Id Id { get { return Client.Id; } }
        public bool IsConnected { get { return Client.IsConnected; } }
        public bool IsAuthenticated { get { return Client.IsAuthenticated; } }

        public SyncClientConnection()
        {
            Client = new ClientConnection();
            Client.Connected += (s, e) => { connectEvent.Set(); };
            Client.Disconnected += (s, e) => { disconnectEvent.Set(); };
            Client.SignedIn += (s, e) => { signInEvent.Set(); };
            Client.AuthenticationFailed += (s, e) => { signInEvent.Set(); };
            Client.SignedOut += (s, e) => { signOutEvent.Set(); };
            Client.PacketReceived += (s, e) =>
            {
                packets.Enqueue(e.Packet);
                receiveEvent.Set();
            };
        }

        public IEnumerable<IClientModule> Modules { get { return Client.Modules; } }

        public void AddModule(IClientModule module)
        {
            Client.AddModule(module);
        }

        public void RemoveModule(IClientModule module)
        {
            Client.RemoveModule(module);
        }


        public async Task Connect(IClientChannel channel)
        {
            await Task.Factory.StartNew(() =>
            {
                connectEvent.Reset();
                Client.Connect(channel);
                connectEvent.WaitOne();
            });
        }

        public async Task Connect(string endpoint, X509Certificate2 certificate = null)
        {
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    connectEvent.Reset();
                    Client.Connect(endpoint, certificate);
                    connectEvent.WaitOne();
                }
                catch
                {
                    connectEvent.Set();
                }
            });
        }

        public async Task Connect<T>(string endpoint, X509Certificate2 certificate = null) where T : IPacketSerializer, new()
        {
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    connectEvent.Reset();
                    Client.Connect<T>(endpoint, certificate);
                    connectEvent.WaitOne();
                }
                catch
                {
                    connectEvent.Set();
                }
            });
        }

        public async Task Disconnect()
        {
            if (IsConnected)
            {
                await Task.Factory.StartNew(() =>
                {
                    disconnectEvent.Reset();
                    Client.Disconnect();
                    disconnectEvent.WaitOne();
                });
            }
        }


        public async Task SignIn(SignIn signIn)
        {
            if (IsConnected)
            {
                await Task.Factory.StartNew(() =>
                {
                    signInEvent.Reset();
                    Client.SignIn(signIn);
                    signInEvent.WaitOne();
                });
            }
        }

        public async Task SignIn(Id id)
        {
            if (IsConnected)
            {
                await Task.Factory.StartNew(() =>
                {
                    signInEvent.Reset();
                    Client.SignIn(id);
                    signInEvent.WaitOne();
                });
            }
        }

        public async Task RestoreSession(Session session)
        {
            if (IsConnected)
            {
                await Task.Factory.StartNew(() =>
                {
                    signInEvent.Reset();
                    Client.RestoreSession(session);
                    signInEvent.WaitOne();
                });
            }
        }

        public async Task SignOut(Session session)
        {
            if (IsConnected && IsAuthenticated)
            {
                await Task.Factory.StartNew(() =>
                {
                    signOutEvent.Reset();
                    Client.SignOut(session);
                    signOutEvent.WaitOne();
                });
            }
        }


        public async Task Send(object content)
        {
            if (IsConnected)
                await Task.Factory.StartNew(() =>
                {
                    Client.Send(content);
                });
        }

        public async Task Send(Packet packet)
        {
            if (IsConnected)
                await Task.Factory.StartNew(() =>
                {
                    Client.Send(packet);
                });
        }

        public async Task<Packet> Receive()
        {
            if (IsConnected)
            {
                if (packets.Count == 0)
                    receiveEvent.Reset();
                await Task.Factory.StartNew(() =>
                {
                    receiveEvent.WaitOne();
                });
                return packets.Dequeue();
            }
            return null;
        }

        public void Dispose()
        {
            Disconnect().Wait();
            connectEvent.Dispose();
            disconnectEvent.Dispose();
            signInEvent.Dispose();
            signOutEvent.Dispose();
            receiveEvent.Dispose();
            packets.Clear();
            connectEvent = null;
            disconnectEvent = null;
            signInEvent = null;
            signOutEvent = null;
            receiveEvent = null;
            packets = null;
            Client = null;
        }
    }
}
