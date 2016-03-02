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
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace LinkupSharp.Channels
{
    public class WebSocketClientChannel<T> : IClientChannel where T : IPacketSerializer, new()
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WebSocketClientChannel<T>));

        private WebSocket socket;
        private IPacketSerializer serializer;
        private Task readingTask;
        private CancellationTokenSource cancel;
        private X509Certificate2 certificate;

        public WebSocketClientChannel(string url, X509Certificate2 certificate = null)
        {
            this.certificate = certificate;
            ServicePointManager.ServerCertificateValidationCallback = CertificateValidation;
            ClientWebSocket socket = new ClientWebSocket();
            socket.ConnectAsync(new Uri(url), CancellationToken.None).Wait();
            SetSocket(socket);
        }

        #region Methods

        private bool CertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (this.certificate == null) return false;
            return certificate.GetSerialNumberString().Equals(this.certificate.GetSerialNumberString());
        }

        private void SetSocket(WebSocket socket)
        {
            this.socket = socket;
            byte[] token = new byte[] { 0x0007, 0x000C, 0x000B };
            serializer = new TokenizedPacketSerializer<T>(token);
            cancel = new CancellationTokenSource();
            readingTask = Task.Factory.StartNew(Read);
        }

        private void Read()
        {
            while ((cancel != null) && (!cancel.IsCancellationRequested))
            {
                try
                {
                    byte[] buffer = new byte[65536];
                    var result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancel.Token).Result;
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        cancel.Cancel();
                        continue;
                    }
                    Packet packet = serializer.Deserialize(buffer.Take(result.Count).ToArray());
                    while (packet != null)
                    {
                        OnPacketReceived(packet);
                        packet = serializer.Deserialize(new byte[0]);
                    }
                }
                catch { }
            }
        }

        public async Task Send(Packet packet)
        {
            if ((cancel != null) && (!cancel.IsCancellationRequested))
            {
                try
                {
                    byte[] buffer = serializer.Serialize(packet);
                    await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, cancel.Token);
                }
                catch (Exception ex)
                {
                    log.Error("Sending error", ex);
                    await Close();
                }
            }
        }

        public async Task Close()
        {
            if ((cancel != null) && (!cancel.IsCancellationRequested))
            {
                cancel.Cancel();
                if (readingTask != null)
                {
                    await readingTask;
                    readingTask.Dispose();
                }
                if (socket != null)
                    socket.Dispose();
                cancel.Dispose();
                OnClosed();
            }
        }

        public void Dispose()
        {
            Close();
        }

        #endregion Methods

        #region Events

        public event EventHandler<PacketEventArgs> PacketReceived;
        public event EventHandler<EventArgs> Closed;

        private void OnPacketReceived(Packet packet)
        {
            if (PacketReceived != null)
                PacketReceived(this, new PacketEventArgs(packet));
        }

        private void OnClosed()
        {
            if (Closed != null)
                Closed(this, EventArgs.Empty);
        }

        #endregion Events

    }
}
