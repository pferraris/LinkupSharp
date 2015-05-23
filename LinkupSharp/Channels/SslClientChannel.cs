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

using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace LinkupSharp.Channels
{
    public class SslClientChannel : TcpClientChannel
    {
        private X509Certificate2 certificate;

        public SslClientChannel(string host, int port, X509Certificate2 certificate)
            : base()
        {
            this.certificate = certificate;
            SetSocket(new TcpClient(host, port));
        }

        internal SslClientChannel(X509Certificate2 certificate)
            : base()
        {
            this.certificate = certificate;
        }

        protected override Stream GetStream()
        {
            if (certificate == null) throw new InvalidOperationException("You must provide a certificate for ssl connection.");
            SslStream stream = null;
            if (ServerSide)
            {
                stream = new SslStream(Socket.GetStream(), false);
                stream.AuthenticateAsServer(certificate);
            }
            else
            {
                stream = new SslStream(Socket.GetStream(), false, CertificateValidation);
                string hostname = Socket.Client.RemoteEndPoint.ToString();
                if (hostname.Contains(":"))
                    hostname = hostname.Substring(0, hostname.IndexOf(':'));
                stream.AuthenticateAsClient(hostname);
            }
            return stream;
        }

        private bool CertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (this.certificate == null) return false;
            return certificate.GetSerialNumberString().Equals(this.certificate.GetSerialNumberString());
        }
    }
}
