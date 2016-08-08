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
    public class WebSocketClientChannel : IClientChannel
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WebSocketClientChannel));
        private static readonly byte[] token = new byte[] { 0x0007, 0x000C, 0x000B };

        private ClientWebSocket socket;
        private IPacketSerializer serializer;
        private Task readingTask;
        private CancellationTokenSource cancel;

        public string Endpoint { get; set; }
        public X509Certificate2 Certificate { get; set; }

        public WebSocketClientChannel()
        {
            ServicePointManager.ServerCertificateValidationCallback = CertificateValidation;
        }

        #region Methods

        public void SetSerializer(IPacketSerializer serializer)
        {
            this.serializer = new TokenizedPacketSerializer(token, serializer);
        }

        public async Task Open()
        {
            if (serializer == null)
                SetSerializer(new JsonPacketSerializer());
            if (string.IsNullOrEmpty(Endpoint)) return;
            socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri(Endpoint), CancellationToken.None);
            cancel = new CancellationTokenSource();
            readingTask = Task.Factory.StartNew(Read);
        }

        private bool CertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (Certificate == null) return false;
            return certificate.GetSerialNumberString().Equals(Certificate.GetSerialNumberString());
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
                        packet = serializer.Deserialize();
                    }
                }
                catch { }
            }
            socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Ok", CancellationToken.None);
            Task.Factory.StartNew(OnClosed);
        }

        public async Task<bool> Send(Packet packet)
        {
            if ((cancel != null) && (!cancel.IsCancellationRequested))
            {
                try
                {
                    byte[] buffer = serializer.Serialize(packet);
                    await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, cancel.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    log.Error("Sending error", ex);
                    await Close();
                }
            }
            return false;
        }

        public async Task Close()
        {
            if ((cancel != null) && (!cancel.IsCancellationRequested))
            {
                cancel.Cancel();
                if (readingTask != null)
                    await readingTask;
            }
        }

        public void Dispose()
        {
            var task = Close();
        }

        #endregion Methods

        #region Events

        public event EventHandler<PacketEventArgs> PacketReceived;
        public event EventHandler<EventArgs> Closed;

        private void OnPacketReceived(Packet packet)
        {
            PacketReceived?.Invoke(this, new PacketEventArgs(packet));
        }

        private void OnClosed()
        {
            if (readingTask != null)
            {
                readingTask.Wait();
                readingTask.Dispose();
            }
            if (socket != null) socket.Dispose();
            if (cancel != null) cancel.Dispose();
            Closed?.Invoke(this, EventArgs.Empty);
        }

        #endregion Events

    }
}
