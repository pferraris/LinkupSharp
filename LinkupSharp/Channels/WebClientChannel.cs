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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace LinkupSharp.Channels
{
    public class WebClientChannel<T> : IClientChannel where T : IPacketSerializer, new()
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WebClientChannel<T>));
        private IPacketSerializer serializer;
        private Task readingTask;
        private bool active;
        private bool serverSide;
        private Queue<Packet> pending;
        private int poolingTime;
        private Timer inactivityTimer;
        private int inactivityTime;
        private string uri;
        private X509Certificate2 certificate;

        internal string Id { get; private set; }

        static WebClientChannel()
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
        }

        internal WebClientChannel(string id, bool serverSide)
        {
            Id = id;
            this.serverSide = true;
            serializer = new T();
            inactivityTime = 5000;
            inactivityTimer = new Timer(state => Close().Wait(), null, inactivityTime, Timeout.Infinite);
            pending = new Queue<Packet>();
        }

        public WebClientChannel(string uri, X509Certificate2 certificate = null)
        {
            WebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
            this.uri = uri;
            this.certificate = certificate;
            ServicePointManager.ServerCertificateValidationCallback = CertificateValidation;
            Id = Guid.NewGuid().ToString();
            serverSide = false;
            serializer = new T();
            poolingTime = 1000;
            inactivityTime = 5000;
        }

        #region Methods

        public async Task Open()
        {
            if (!serverSide)
                await Task.Factory.StartNew(() =>
                {
                    active = true;
                    readingTask = Task.Factory.StartNew(Read);
                });
        }

        private bool CertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (this.certificate == null) return false;
            return certificate.GetSerialNumberString().Equals(this.certificate.GetSerialNumberString());
        }

        internal void DataReceived(byte[] buffer)
        {
            OnPacketReceived(serializer.Deserialize(buffer));
        }

        internal byte[] DataPending()
        {
            inactivityTimer.Change(inactivityTime, Timeout.Infinite);
            lock (pending)
                if (pending.Any())
                    return serializer.Serialize(pending.Dequeue());
            return new byte[0];
        }

        private void Read()
        {
            while (active)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("ClientId", Id);
                        var response = client.GetByteArrayAsync(new Uri(uri)).Result;
                        if (response != null && response.Length > 0)
                            DataReceived(response);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Reading error", ex);
                }
                finally
                {
                    Thread.Sleep(poolingTime);
                }
            }
        }

        public async Task Send(Packet packet)
        {
            if (serverSide)
                lock (pending)
                    pending.Enqueue(packet);
            else if (active)
            {
                try
                {
                    byte[] buffer = serializer.Serialize(packet);
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("ClientId", Id);
                        var content = new ByteArrayContent(buffer);
                        var response = await client.PostAsync(uri, content);
                        if (!response.IsSuccessStatusCode)
                            throw new InvalidOperationException($"StatusCode: {response.StatusCode}");
                    }
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
            if (serverSide)
            {
                try
                {
                    inactivityTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    inactivityTimer.Dispose();
                }
                catch { }
            }
            else if (active)
            {
                active = false;
                try
                {
                    await readingTask;
                    readingTask.Dispose();
                }
                catch { }
            }
            await Task.Factory.StartNew(OnClosed);
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
            PacketReceived?.Invoke(this, new PacketEventArgs(packet));
        }

        private void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        #endregion Events

    }
}
