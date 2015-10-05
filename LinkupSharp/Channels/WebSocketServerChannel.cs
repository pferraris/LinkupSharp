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
using SocketHttpListener;
using System;
using System.Text;
using System.Threading.Tasks;

namespace LinkupSharp.Channels
{
    internal class WebSocketServerChannel : IClientChannel
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WebSocketClientChannel));
        private static readonly byte[] Token = new byte[] { 0x0007, 0x000C, 0x000B };

        private WebSocket socket;
        private IPacketSerializer serializer;

        internal WebSocketServerChannel(WebSocket socket)
        {
            this.socket = socket;
            socket.OnOpen += Socket_OnOpen;
            socket.OnClose += Socket_OnClose;
            socket.OnMessage += Socket_OnMessage;
            socket.OnError += Socket_OnError;
            serializer = new JsonPacketSerializer();
            socket.ConnectAsServer();
        }

        private void Socket_OnOpen(object sender, EventArgs e)
        {
            
        }

        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            socket = null;
            OnClosed();
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                if (e.Type == Opcode.Close)
                    socket.Close();
                else
                    foreach (var packet in serializer.Deserialize(e.RawData, Token))
                        OnPacketReceived(packet);
            }
            catch (Exception ex)
            {
                log.Error("WebSocket OnMessage error", ex);
            }
        }

        private void Socket_OnError(object sender, ErrorEventArgs e)
        {
            log.Error(e.Message);
        }

        #region Methods

        public void Send(Packet packet)
        {
            if (socket.ReadyState == WebSocketState.Open)
            {
                try
                {
                    byte[] buffer = serializer.Serialize(packet, Token);
                    socket.SendAsync(Encoding.UTF8.GetString(buffer), x => { });
                }
                catch (Exception ex)
                {
                    log.Error("Sending error", ex);
                    Close();
                }
            }
        }

        public void Close()
        {
            if ((socket.ReadyState == WebSocketState.Open) && (socket.ReadyState == WebSocketState.Connecting))
                socket.Close();
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
                Task.Run(() => PacketReceived(this, new PacketEventArgs(packet)));
        }

        private void OnClosed()
        {
            if (Closed != null)
                Task.Run(() => Closed(this, EventArgs.Empty));
        }

        #endregion Events

    }
}
