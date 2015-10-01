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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace LinkupSharp.Channels
{
    public class WebChannelListener : IChannelListener
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WebChannelListener));
        public string Prefix { get; private set; }

        private Dictionary<string, WebClientChannel> connections;
        private HttpListener listener;
        private bool listening;
        private Task listenerTask;

        public WebChannelListener(string prefix)
        {
            Prefix = prefix;
        }

        #region Methods

        public void Start()
        {
            connections = new Dictionary<string, WebClientChannel>();
            listener = new HttpListener();
            listener.Prefixes.Add(Prefix);
            listener.Start();
            listening = true;
            listenerTask = Task.Factory.StartNew(Listen);
        }

        public void Stop()
        {
            listening = false;
            listener.Stop();
            listenerTask.Wait();
            listener = null;
            listenerTask.Dispose();
            listenerTask = null;
        }

        private void Listen()
        {
            while (listening)
            {
                try
                {
                    var context = listener.GetContext();
                    Task.Run(() => ProcessRequest(context));
                }
                catch (Exception ex)
                {
                    log.Error("Error when client connected", ex);
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            if (context == null) return;
            try
            {
                if (!context.Request.Headers.AllKeys.Contains("ClientId")) return;
                string id = context.Request.Headers["ClientId"];
                lock (connections)
                if (!connections.ContainsKey(id))
                    {
                        var client = new WebClientChannel(id, true);
                        client.Closed += client_Closed;
                        connections.Add(id, client);
                        OnClientConnected(client);
                    }
                if (context.Request.HttpMethod == "POST")
                {
                    byte[] buffer = new byte[65536];
                    int count;
                    do
                    {
                        count = context.Request.InputStream.Read(buffer, 0, buffer.Length);
                        if (count > 0) connections[id].DataReceived(buffer.Take(count).ToArray());
                    } while (count > 0);
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                }
                else if (context.Request.HttpMethod == "GET")
                {
                    byte[] buffer = connections[id].DataPending();
                    if (buffer.Any())
                    {
                        context.Response.ContentLength64 = buffer.Length;
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Flush();
                        context.Response.OutputStream.Close();
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                    }
                    else
                        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                }
                else
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            }
            catch (Exception ex)
            {
                log.Error("Error processing request", ex);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            context.Response.Close();
        }

        void client_Closed(object sender, EventArgs e)
        {
            var client = sender as WebClientChannel;
            if (connections.ContainsKey(client.Id))
                connections.Remove(client.Id);
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
