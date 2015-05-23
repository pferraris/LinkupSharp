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

using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace LinkupSharp.Channels
{
    public class WebSocketChannelListener : IChannelListener
    {
        public event EventHandler<ClientChannelEventArgs> ClientConnected;

        public string Prefix { get; private set; }

        private HttpListener listener;
        private CancellationTokenSource cancel;
        private Task listenerTask;

        public WebSocketChannelListener(string prefix)
        {
            Prefix = prefix;
        }

        public void Start()
        {
            if (listener == null)
            {
                listener = new HttpListener();
                listener.Prefixes.Add(Prefix);
                listener.Start();
                cancel = new CancellationTokenSource();
                listenerTask = Task.Factory.StartNew(Listen);
            }
        }

        public void Stop()
        {
            if (listener != null)
            {
                cancel.Cancel();
                listener.Stop();
                listenerTask.Wait();
                listenerTask.Dispose();
                listener = null;
                listenerTask = null;
                cancel = null;
            }
        }

        private void Listen()
        {
            HttpListenerContext context;
            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    context = listener.GetContext();
                }
                catch
                {
                    context = null;
                }
                if (context != null)
                {
                    if (context.Request.IsWebSocketRequest)
                    {
                        ProcessRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        context.Response.Close();
                    }
                }
            }
        }

        private async void ProcessRequest(HttpListenerContext listenerContext)
        {
            WebSocketContext webSocketContext = null;
            try
            {
                webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
                OnClientConnected(new WebSocketClientChannel(webSocketContext.WebSocket));
            }
            catch
            {
                listenerContext.Response.StatusCode = 500;
                listenerContext.Response.Close();
                return;
            }
        }

        private void OnClientConnected(IClientChannel clientChannel)
        {
            if (ClientConnected != null)
                ClientConnected(this, new ClientChannelEventArgs(clientChannel));
        }
    }
}
