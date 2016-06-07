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
using SocketHttpListener.Net;
using SocketHttpListener.Net.WebSockets;
using System;
using System.Security.Cryptography.X509Certificates;

namespace LinkupSharp.Channels
{
    public class WebSocketChannelListener<T> : IChannelListener where T : IPacketSerializer, new()
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WebSocketChannelListener<T>));
        private HttpListener listener;

        public string Endpoint { get; set; }
        public X509Certificate2 Certificate { get; set; }

        public WebSocketChannelListener()
        {
        }

        #region Methods

        public void Start()
        {
            if (string.IsNullOrEmpty(Endpoint)) return;
            if (listener != null) Stop();
            listener = new HttpListener(Certificate);
            var endpoint = Endpoint.Replace("0.0.0.0", "+");
            endpoint = endpoint.Replace("wss://", "https://");
            endpoint = endpoint.Replace("ws://", "http://");
            listener.Prefixes.Add(endpoint);
            listener.OnContext = x => ProcessRequest(x);
            listener.Start();
        }

        public void Stop()
        {
            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }
        }

        private void ProcessRequest(HttpListenerContext listenerContext)
        {
            try
            {
                if (listenerContext.Request.IsWebSocketRequest)
                {
                    WebSocketContext webSocketContext = null;
                    webSocketContext = listenerContext.AcceptWebSocket(null);
                    OnClientConnected(new WebSocketServerChannel<T>(webSocketContext.WebSocket));
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

        public event EventHandler<ClientChannelEventArgs> ClientConnected;

        private void OnClientConnected(IClientChannel clientChannel)
        {
            if (clientChannel == null) return;
            ClientConnected?.Invoke(this, new ClientChannelEventArgs(clientChannel));
        }

        #endregion Events

    }
}
