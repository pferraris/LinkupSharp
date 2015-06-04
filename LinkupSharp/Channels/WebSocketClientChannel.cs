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

using LinkupSharp.Loggers;
using LinkupSharp.Serializers;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace LinkupSharp.Channels
{
    public class WebSocketClientChannel : IClientChannel
    {
        private static readonly byte[] Token = new byte[] { 0x0007, 0x000C, 0x000B };
     
        private WebSocket socket;
        private IPacketSerializer serializer;
        private Task readingTask;
        private CancellationTokenSource cancel;


        public WebSocketClientChannel(string url)
        {
            ClientWebSocket socket = new ClientWebSocket();
            Task connecting = socket.ConnectAsync(new Uri(url), CancellationToken.None);
            connecting.Wait();
            SetSocket(socket);
        }

        internal WebSocketClientChannel(WebSocket socket)
        {
            SetSocket(socket);
        }

        #region Methods

        private void SetSocket(WebSocket socket)
        {
            this.socket = socket;
            serializer = new JsonPacketSerializer();
            cancel = new CancellationTokenSource();
            readingTask = Task.Factory.StartNew(Read, cancel.Token);
        }

        private void Read()
        {
            while ((cancel != null) && (!cancel.IsCancellationRequested))
            {
                try
                {
                    byte[] buffer = new byte[65536];
                    var receiving = socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancel.Token);
                    receiving.Wait();
                    if (receiving.Result.MessageType == WebSocketMessageType.Close)
                    {
                        Close();
                        continue;
                    }
                    foreach (var packet in serializer.Deserialize(buffer.Take(receiving.Result.Count).ToArray(), Token))
                        OnPacketReceived(packet);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Reading error.");
                }
            }
        }

        public void Send(Packet packet)
        {
            if ((cancel != null) && (!cancel.IsCancellationRequested))
                try
                {
                    lock (socket)
                    {
                        byte[] buffer = serializer.Serialize(packet, Token);
                        socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancel.Token);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Sending error.");
                    Close();
                }
        }

        public void Close()
        {
            if ((cancel != null) && (!cancel.IsCancellationRequested))
            {
                cancel.Cancel();
                if (readingTask != null)
                {
                    readingTask.Wait();
                    readingTask.Dispose();
                }
                if (socket != null)
                {
                    socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait();
                    socket.Dispose();
                }
                readingTask = null;
                socket = null;
                cancel = null;
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
