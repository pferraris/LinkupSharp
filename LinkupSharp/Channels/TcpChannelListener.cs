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

using LinkupSharp.Serializers;
using log4net;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace LinkupSharp.Channels
{
    public class TcpChannelListener<T> : IChannelListener where T : IPacketSerializer, new()
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TcpChannelListener<T>));
        public int Port { get; private set; }
        public IPAddress Address { get; private set; }

        private TcpListener listener;
        private bool listening;
        private Task listenerTask;
        private X509Certificate2 certificate;

        public TcpChannelListener(int port)
            : this(port, null, null)
        {
        }

        public TcpChannelListener(int port, IPAddress address)
            : this(port, address, null)
        {
        }

        public TcpChannelListener(int port, X509Certificate2 certificate)
            : this(port, null, certificate)
        {
        }

        public TcpChannelListener(int port, IPAddress address, X509Certificate2 certificate)
        {
            Port = port;
            if (address == null)
                Address = IPAddress.Any;
            else
                Address = address;
            this.certificate = certificate;
        }

        #region Methods

        public void Start()
        {
            listener = new TcpListener(Address, Port);
            listener.Start();
            listening = true;
            listenerTask = Task.Factory.StartNew(Listen);
        }

        public void Stop()
        {
            listening = false;
            listenerTask.Wait();
            listener.Stop();
            listener = null;
            listenerTask.Dispose();
            listenerTask = null;
        }

        private void Listen()
        {
            while (listening)
            {
                if (listener.Pending())
                    try
                    {
                        OnClientConnected(CreateClient(listener.AcceptTcpClient()));
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error when client connected", ex);
                    }
                Thread.Sleep(50);
            }
        }

        private IClientChannel CreateClient(TcpClient socket)
        {
            try
            {
                var client = new TcpClientChannel<T>(certificate);
                client.SetSocket(socket, true);
                return client;
            }
            catch (Exception ex)
            {
                log.Error("Cannot create client connection", ex);
                socket.Close();
                socket.GetStream().Close();
                socket.Client.Disconnect(false);
                socket.Client.Dispose();
                return null;
            }
        }

        #endregion Methods

        #region Events

        public event EventHandler<ClientChannelEventArgs> ClientConnected;

        private void OnClientConnected(IClientChannel clientChannel)
        {
            if (clientChannel == null) return;
            if (ClientConnected != null)
                ClientConnected(this, new ClientChannelEventArgs(clientChannel));
        }

        #endregion Events

    }
}
