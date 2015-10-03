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

using log4net;
using SocketHttpListener.Net;
using SocketHttpListener.Net.WebSockets;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace LinkupSharp.Channels
{
    public class WebSocketChannelListener : IChannelListener
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WebSocketChannelListener));
        private HttpListener listener;
        private X509Certificate2 certificate;

        public event EventHandler<ClientChannelEventArgs> ClientConnected;
        public string Prefix { get; private set; }

        public WebSocketChannelListener(string prefix, X509Certificate2 certificate = null)
        {
            Prefix = prefix;
            this.certificate = certificate;
        }

        public void Start()
        {
            if (listener != null)
                Stop();
            listener = new HttpListener(new Patterns.Logging.NullLogger(), certificate);
            listener.Prefixes.Add(Prefix);
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
                    OnClientConnected(new WebSocketServerChannel(webSocketContext.WebSocket));
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

        private void OnClientConnected(IClientChannel clientChannel)
        {
            if (ClientConnected != null)
                ClientConnected(this, new ClientChannelEventArgs(clientChannel));
        }
    }
}
