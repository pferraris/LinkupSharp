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
    internal class WebSocketServerChannel<T> : IClientChannel where T : IPacketSerializer, new()
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WebSocketServerChannel<T>));
        private static readonly byte[] token = new byte[] { 0x0007, 0x000C, 0x000B };

        private WebSocket socket;
        private IPacketSerializer serializer;

        internal WebSocketServerChannel(WebSocket socket)
        {
            this.socket = socket;
            socket.OnOpen += Socket_OnOpen;
            socket.OnClose += Socket_OnClose;
            socket.OnMessage += Socket_OnMessage;
            serializer = new TokenizedPacketSerializer<T>(token);
            socket.ConnectAsServer();
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
                if (e.Type == Opcode.Close)
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

        public async Task Open()
        {
            await Task.FromResult<object>(null);
        }

        public async Task Send(Packet packet)
        {
            if (socket.ReadyState == WebSocketState.Open)
            {
                try
                {
                    byte[] buffer = serializer.Serialize(packet);
                    socket.SendAsync(buffer, null);
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
            if ((socket.ReadyState == WebSocketState.Open) && (socket.ReadyState == WebSocketState.Connecting))
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
