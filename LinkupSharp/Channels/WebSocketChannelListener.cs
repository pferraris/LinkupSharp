#region License
/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2017 Pablo Ferraris
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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WebSocketSharp.Net;
using WebSocketSharp.Net.WebSockets;

namespace LinkupSharp.Channels
{
    public class WebSocketChannelListener : IChannelListener
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WebSocketChannelListener));
        private HttpListener listener;
        private bool listening;
        private Task listenerTask;
        private IPacketSerializer serializer;

        public string Endpoint { get; set; }
        public X509Certificate2 Certificate { get; set; }

        public WebSocketChannelListener()
        {
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
            if (listener != null) Stop();
            listener = new HttpListener();
            listener.SslConfiguration.ServerCertificate = Certificate;
            var endpoint = Endpoint.Replace("0.0.0.0", "+");
            endpoint = endpoint.Replace("wss://", "https://");
            endpoint = endpoint.Replace("ws://", "http://");
            listener.Prefixes.Add(endpoint);
            listener.Start();
            listening = true;
            listenerTask = Task.Factory.StartNew(Listen);
        }

        public void Stop()
        {
            if (listener != null)
            {
                listening = false;
                listener.Stop();
                listenerTask.Wait();
                listenerTask.Dispose();
                listener = null;
            }
        }

        private void Listen()
        {
            while (listening)
            {
                var ares = listener.BeginGetContext(x => ProcessRequest(listener.EndGetContext(x)), null);
                ares.AsyncWaitHandle.WaitOne();
            }
        }

        private void ProcessRequest(HttpListenerContext listenerContext)
        {
            try
            {
                if (listenerContext.Request.IsWebSocketRequest)
                {
                    HttpListenerWebSocketContext webSocketContext = null;
                    webSocketContext = listenerContext.AcceptWebSocket(null);
                    var channel = new WebSocketChannel(webSocketContext.WebSocket);
                    channel.SetSerializer(serializer);
                    OnClientConnected(channel);
                }
                else
                {
                    listenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    listenerContext.Response.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error("Error processing HttpListenerContext for WebSocket", ex);
                listenerContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                listenerContext.Response.Close();
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
