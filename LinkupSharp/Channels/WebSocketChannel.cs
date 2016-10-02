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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WebSocketSharp;

namespace LinkupSharp.Channels
{
    internal class WebSocketChannel : IChannel
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WebSocketChannel));
        private static readonly byte[] token = new byte[] { 0x0007, 0x000C, 0x000B };

        private WebSocket socket;
        private IPacketSerializer serializer;
        private bool isServerSide;

        public string Endpoint { get; set; }
        public X509Certificate2 Certificate { get; set; }

        internal WebSocketChannel(WebSocket socket)
        {
            this.socket = socket;
            socket.OnOpen += Socket_OnOpen;
            socket.OnClose += Socket_OnClose;
            socket.OnMessage += Socket_OnMessage;
            isServerSide = true;
        }

        public WebSocketChannel()
        {
            isServerSide = false;
        }

        #region Socket Events

        private void Socket_OnOpen(object sender, EventArgs e)
        {

        }

        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            OnClosed();
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.IsClose)
                    socket.Close();
                else
                {
                    var packet = serializer.Deserialize(e.RawData);
                    while (packet != null)
                    {
                        OnPacketReceived(packet);
                        packet = serializer.Deserialize();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("WebSocket OnMessage error", ex);
            }
        }

        #endregion Socket Events

        #region Methods

        private bool CertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (Certificate == null) return false;
            return certificate.GetSerialNumberString().Equals(Certificate.GetSerialNumberString());
        }

        public void SetSerializer(IPacketSerializer serializer)
        {
            this.serializer = new TokenizedPacketSerializer(token, serializer);
        }

        public async Task Open()
        {
            await Task.Yield();
            if (serializer == null)
                SetSerializer(new JsonPacketSerializer());
            if (isServerSide)
                socket.Accept();
            else
            {
                if (string.IsNullOrEmpty(Endpoint)) return;
                socket = new WebSocket(Endpoint);
                socket.OnOpen += Socket_OnOpen;
                socket.OnClose += Socket_OnClose;
                socket.OnMessage += Socket_OnMessage;
                socket.SslConfiguration.ServerCertificateValidationCallback = CertificateValidation;
                socket.Connect();
            }
        }

        public async Task<bool> Send(Packet packet)
        {
            if (socket.ReadyState == WebSocketState.Open)
            {
                try
                {
                    byte[] buffer = serializer.Serialize(packet);
                    socket.SendAsync(buffer, null);
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
            if ((socket.ReadyState == WebSocketState.Open) || (socket.ReadyState == WebSocketState.Connecting))
                await Task.Factory.StartNew(socket.Close);
        }

        public void Dispose()
        {
            Close().Wait();
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
