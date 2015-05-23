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
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace LinkupSharp.Channels
{
    public class SslChannelListener : TcpChannelListener
    {
        private X509Certificate2 certificate;

        public SslChannelListener(int port, X509Certificate2 certificate, IPAddress address = null)
            : base(port, address)
        {
            this.certificate = certificate;
        }

        protected override IClientChannel CreateClient(TcpClient socket)
        {
            try
            {
                SslClientChannel client = new SslClientChannel(certificate);
                client.SetSocket(socket, true);
                return client;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Cannot create client connection.");
                socket.Close();
                socket.GetStream().Close();
                socket.Client.Disconnect(false);
                socket.Client.Dispose();
                return null;
            }
        }
    }
}
