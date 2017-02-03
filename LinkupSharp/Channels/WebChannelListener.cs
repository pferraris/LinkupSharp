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
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WebSocketSharp.Net;

namespace LinkupSharp.Channels
{
    public class WebChannelListener : IChannelListener
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WebChannelListener));

        private HttpListener listener;
        private bool listening;
        private Task listenerTask;
        private Dictionary<string, WebChannel> connections;
        private IPacketSerializer serializer;

        public string Endpoint { get; set; }
        public X509Certificate2 Certificate { get; set; }

        public WebChannelListener()
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
            connections = new Dictionary<string, WebChannel>();
            listener = new HttpListener();
            listener.SslConfiguration.ServerCertificate = Certificate;
            var endpoint = Endpoint.Replace("0.0.0.0", "+");
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
                        var client = new WebChannel(id);
                        client.SetSerializer(serializer);
                        client.Closed += client_Closed;
                        connections.Add(id, client);
                        OnClientConnected(client);
                    }
                if (context.Request.HttpMethod == "POST")
                {
                    List<byte> content = new List<byte>();
                    int count;
                    do
                    {
                        byte[] buffer = new byte[65536];
                        count = context.Request.InputStream.Read(buffer, 0, buffer.Length);
                        content.AddRange(buffer.Take(count));
                    } while (count > 0);
                    connections[id].DataReceived(content.ToArray());
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                }
                else if (context.Request.HttpMethod == "GET")
                {
                    byte[] buffer = connections[id].DataPending();
                    if (buffer.Length > 0)
                    {
                        context.Response.ContentLength64 = buffer.Length;
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
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
            var client = sender as WebChannel;
            if (connections.ContainsKey(client.Id))
                connections.Remove(client.Id);
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
