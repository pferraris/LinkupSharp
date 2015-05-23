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

using LinkupSharp.Loggers;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LinkupSharp.Channels
{
    public class TcpChannelListener : IChannelListener
    {
        public int Port { get; private set; }
        public IPAddress Address { get; private set; }

        private TcpListener listener;
        private bool listening;
        private Task listenerTask;

        public TcpChannelListener(int port, IPAddress address = null)
        {
            Port = port;
            if (address == null)
                Address = IPAddress.Any;
            else
                Address = address;
        }

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
                        Logger.Error(ex, "Error when client connected.");
                    }
                Thread.Sleep(50);
            }
        }

        protected virtual IClientChannel CreateClient(TcpClient socket)
        {
            try
            {
                TcpClientChannel client = new TcpClientChannel();
                client.SetSocket(socket, true);
                return client;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Cannot create client connection.");
                socket.Close();
                socket.GetStream().Close();
                socket.Client.Disconnect(false);
                socket.Client.Dispose();
                return null;
            }
        }

        #region Events

        public event EventHandler<ClientChannelEventArgs> ClientConnected;

        private void OnClientConnected(IClientChannel clientChannel)
        {
            if (clientChannel == null) return;
            if (ClientConnected != null)
                Task.Factory.StartNew(() =>
                ClientConnected(this, new ClientChannelEventArgs(clientChannel)));
        }

        #endregion Events

    }
}
