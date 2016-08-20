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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace LinkupSharp.Channels
{
    public class TcpChannelListener : IChannelListener
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TcpChannelListener));

        private List<TcpListener> listeners;
        private bool listening;
        private Task listenerTask;
        private IPacketSerializer serializer;

        public string Endpoint { get; set; }
        public X509Certificate2 Certificate { get; set; }


        public TcpChannelListener()
        {
            listeners = new List<TcpListener>();
        }

        #region Methods

        public void SetSerializer(IPacketSerializer serializer)
        {
            this.serializer = serializer;
        }

        public void Start()
        {
            if (serializer == null)
                serializer = new JsonPacketSerializer();
            if (string.IsNullOrEmpty(Endpoint)) return;
            if (listeners.Count > 0) Stop();
            var endpoint = Endpoint.Replace("+", "0.0.0.0");
            var uri = new Uri(endpoint);
            IPAddress address;
            if (IPAddress.TryParse(uri.Host, out address))
            {
                listeners.Add(new TcpListener(address, uri.Port));
            }
            else
            {
                try
                {
                    IPAddress[] addressList = Dns.GetHostAddresses(uri.Host);
                    foreach (var item in addressList)
                        listeners.Add(new TcpListener(item, uri.Port));
                }
                catch
                {
                    listeners.Add(new TcpListener(IPAddress.Any, uri.Port));
                }
            }
            foreach (var listener in listeners)
                listener.Start();
            listening = true;
            listenerTask = Task.Factory.StartNew(Listen);
        }

        public void Stop()
        {
            listening = false;
            listenerTask.Wait();
            foreach (var listener in listeners.ToArray())
            {
                listener.Stop();
                listeners.Remove(listener);
            }
            listenerTask.Dispose();
            listenerTask = null;
        }

        private void Listen()
        {
            while (listening)
            {
                foreach (var listener in listeners)
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
                }
                Thread.Sleep(50);
            }
        }

        private IChannel CreateClient(TcpClient socket)
        {
            try
            {
                var channel = new TcpChannel(socket, Certificate);
                channel.SetSerializer(serializer);
                return channel;
            }
            catch (Exception ex)
            {
                log.Error("Cannot create client connection", ex);
                socket.Close();
                return null;
            }
        }

        #endregion Methods

        #region Events

        public event EventHandler<ChannelEventArgs> ChannelConnected;

        private void OnClientConnected(IChannel clientChannel)
        {
            if (clientChannel == null) return;
            ChannelConnected?.Invoke(this, new ChannelEventArgs(clientChannel));
        }

        #endregion Events

    }
}
