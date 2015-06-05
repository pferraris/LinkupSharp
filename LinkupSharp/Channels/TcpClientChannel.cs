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
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LinkupSharp.Channels
{
    public class TcpClientChannel : IClientChannel
    {
        private static readonly byte[] Token = new byte[] { 0x0007, 0x000C, 0x000B };

        private Task readingTask;
        private bool active;

        protected TcpClient Socket { get; private set; }
        protected IPacketSerializer Serializer { get; set; }
        protected Stream Stream { get; private set; }
        protected bool ServerSide { get; private set; }

        public TcpClientChannel(string host, int port)
            : this()
        {
            SetSocket(new TcpClient(host, port));
        }

        internal TcpClientChannel() { }

        #region Methods

        internal void SetSocket(TcpClient socket, bool serverSide = false)
        {
            ServerSide = serverSide;
            Serializer = new JsonPacketSerializer();
            if (ServerSide)
                Logger.Info("Connection received from: {0}", socket.Client.RemoteEndPoint);
            Socket = socket;
            Stream = GetStream();
            active = true;
            readingTask = Task.Factory.StartNew(Read);
        }

        protected virtual Stream GetStream()
        {
            return Socket.GetStream();
        }

        private void Read()
        {
            while (active)
            {
                if (Socket.Available > 0)
                {
                    try
                    {
                        byte[] buffer = new byte[65536];
                        int count = Stream.Read(buffer, 0, buffer.Length);
                        foreach (var packet in Serializer.Deserialize(buffer.Take(count).ToArray(), Token))
                            OnPacketReceived(packet);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Reading error.");
                    }
                }
                else
                    Thread.Sleep(50);
            }
        }

        public void Send(Packet packet)
        {
            if (active)
                try
                {
                    lock (Stream)
                    {
                        byte[] buffer = Serializer.Serialize(packet, Token);
                        Stream.Write(buffer, 0, buffer.Length);
                        Stream.Flush();
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
            if (active)
            {
                active = false;
                try
                {
                    readingTask.Wait();
                    readingTask.Dispose();
                }
                catch { }
                try
                {
                    Stream.Close();
                    Stream.Dispose();
                    Socket.Close();
                }
                catch { }
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
