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
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace LinkupSharp.Channels
{
    public class TcpClientChannel<T> : IClientChannel where T : IPacketSerializer, new()
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TcpClientChannel<T>));
        private Task readingTask;
        private bool active;
        private TcpClient socket;
        private IPacketSerializer serializer;
        private Stream stream;
        private bool serverSide;
        private X509Certificate2 certificate;

        public TcpClientChannel(string host, int port)
            : this(host, port, null)
        {
        }

        public TcpClientChannel(string host, int port, X509Certificate2 certificate)
            : this(certificate)
        {
            SetSocket(new TcpClient(host, port));
        }

        internal TcpClientChannel(X509Certificate2 certificate)
        {
            this.certificate = certificate;
        }

        #region Methods

        internal void SetSocket(TcpClient socket, bool serverSide = false)
        {
            this.serverSide = serverSide;
            byte[] token = new byte[] { 0x0007, 0x000C, 0x000B };
            serializer = new TokenizedPacketSerializer<T>(token);
            this.socket = socket;
            stream = GetStream();
            active = true;
            readingTask = Task.Factory.StartNew(Read);
        }

        private Stream GetStream()
        {
            Stream stream = null;
            if (certificate == null)
            {
                stream = socket.GetStream();
            }
            else
            {
                if (serverSide)
                {
                    stream = new SslStream(socket.GetStream(), false);
                    (stream as SslStream).AuthenticateAsServer(certificate);
                }
                else
                {
                    stream = new SslStream(socket.GetStream(), false, CertificateValidation);
                    string hostname = socket.Client.RemoteEndPoint.ToString();
                    if (hostname.Contains(":"))
                        hostname = hostname.Substring(0, hostname.IndexOf(':'));
                    (stream as SslStream).AuthenticateAsClient(hostname);
                }
            }
            return stream;
        }

        private bool CertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (this.certificate == null) return false;
            return certificate.GetSerialNumberString().Equals(this.certificate.GetSerialNumberString());
        }

        private void Read()
        {
            while (active)
            {
                if (socket.Available > 0)
                {
                    try
                    {
                        byte[] buffer = new byte[65536];
                        int count = stream.Read(buffer, 0, buffer.Length);
                        Packet packet = serializer.Deserialize(buffer.Take(count).ToArray());
                        while (packet != null)
                        {
                            OnPacketReceived(packet);
                            packet = serializer.Deserialize(new byte[0]);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Reading error", ex);
                    }
                }
                else
                    Thread.Sleep(50);
            }
        }

        public async Task Send(Packet packet)
        {
            if (active)
                try
                {
                    byte[] buffer = serializer.Serialize(packet);
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                    stream.Flush();
                }
                catch (Exception ex)
                {
                    log.Error("Sending error", ex);
                    Close();
                }
        }

        public async Task Close()
        {
            if (active)
            {
                active = false;
                try
                {
                    await readingTask;
                    readingTask.Dispose();
                }
                catch { }
                try
                {
                    stream.Close();
                    stream.Dispose();
                    socket.Close();
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
